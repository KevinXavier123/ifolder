/***********************************************************************
 *  $RCSfile$
 *
 *  Copyright (C) 2004 Novell, Inc.
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU General Public
 *  License as published by the Free Software Foundation; either
 *  version 2 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public
 *  License along with this program; if not, write to the Free
 *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 *  Author: Mike Lasky <mlasky@novell.com>
 *
 ***********************************************************************/

using System;
using System.Collections;
using System.Security.Cryptography;

using Simias;
using Simias.Event;
using Persist = Simias.Storage.Provider;
using Novell.Security.SecureSink.SecurityProvider.RsaSecurityProvider;

namespace Simias.Storage
{
	/// <summary>
	/// Implements the identity manager which is used to control access to Collection Store objects.
	/// There is only one identity that is allowed to authenticate to the CollectionStore database, 
	/// since the database only ever has one owner.  All other identities can access the database 
	/// only by impersonation.
	/// </summary>
	internal class IdentityManager : RsaKeyStore, IDisposable
	{
		#region Class Members
		/// <summary>
		/// Reference to the store object.
		/// </summary>
		private Store store;

		/// <summary>
		/// Handle to the local address book.
		/// </summary>
		private LocalAddressBook localAb;

		/// <summary>
		/// Name of this domain that the store object belongs in.
		/// </summary>
		private string domainName;

		/// <summary>
		/// Represents the identity of the user that instantiated this object.
		/// </summary>
		private Identity identity;

		/// <summary>
		/// Holds the public key for the server.
		/// </summary>
		private RSACryptoServiceProvider publicKey;

		/// <summary>
		/// Container used to keep track of the current identity for this store handle.
		/// </summary>
		private Stack impersonationId = new Stack();

		/// <summary>
		/// Indicates if the object has been disposed.
		/// </summary>
		private bool disposed = false;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the currently impersonating user guid.
		/// </summary>
		public string CurrentUserGuid
		{
			get 
			{ 
				lock ( store )
				{
					if ( disposed )
					{
						throw new ObjectDisposedException( this.ToString() );
					}

					return ( impersonationId.Count == 0 ) ? identity.Id : ( impersonationId.Peek() as Identity ).Id; 
				}
			}
		}

		/// <summary>
		/// Gets the current impersonating identity.
		/// </summary>
		public Identity CurrentIdentity
		{
			get 
			{ 
				lock ( store )
				{
					if ( disposed )
					{
						throw new ObjectDisposedException( this.ToString() );
					}

					identity.Refresh();
					return ( impersonationId.Count == 0 ) ? identity : impersonationId.Peek() as Identity; 
				}
			}
		}

		/// <summary>
		/// Gets the domain name where this identity exists.
		/// </summary>
		public string DomainName
		{
			get 
			{ 
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				return domainName; 
			}
		}

		/// <summary>
		/// Gets the public key for the owner identity.
		/// </summary>
		public RSACryptoServiceProvider PublicKey
		{
			get 
			{ 
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				return publicKey; 
			}
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Constructor of the object.
		/// </summary>
		/// <param name="identity">Object that represents the current identity.</param>
		public IdentityManager( Identity identity )
		{
			this.identity = identity;
			this.localAb = identity.AddressBook;
			this.store = identity.AddressBook.LocalStore;
			this.domainName = identity.AddressBook.DomainName;
			this.publicKey = new RSACryptoServiceProvider();
			this.publicKey.ImportParameters( identity.ServerCredential.ExportParameters( false ) );

//			// Set up a delegate to update the identity object if it changes.
//			string[] identityFilter = new string[ 1 ];
//			identityFilter[ 0 ] = identity.Id;
//			localAb.NodeEventsSubscribe( new LocalAddressBook.NodeChangeHandler( OnChangedIdentity ), identityFilter );
		}
		#endregion

		#region Private Methods
//		/// <summary>
//		/// This method is informed of a change to the identity object and will automatically refresh it.
//		/// </summary>
//		/// <param name="args">Event context arguments.</param>
//		private void OnChangedIdentity( NodeEventArgs args )
//		{
//			MyTrace.WriteLine( "Refreshing local identity object." );
//			identity.Refresh();
//		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Requests the acceptance of the presented "server" principal credentials.
		/// 
		/// IMPORTANT: The method must raise an exception if there is a security policy in effect that
		/// prevents it from accepting credentials received during peer authentication. Failing to
		/// raise an exception with such a policy in effect will result in a policy violation because
		/// the caller will consider that it is OK to use the credentials to authenticate the peer.
		/// /// </summary>
		/// <param name="realm">The realm to which the server principal belongs</param>
		/// <param name="principalName">The name of the server principal</param>
		/// <param name="rsaKeys">RSA algorithm with the server principal's public key</param>
		public override void AcceptServerPrincipalCredentials(string realm, string principalName, RSACryptoServiceProvider rsaKeys)
		{
			throw new ApplicationException( "Credentials not accepted." );
		}

		/// <summary>
		/// Requests the acceptance of the presented "client" principal credentials.
		/// 
		/// IMPORTANT: The method must raise an exception if there is a security policy in effect that
		/// prevents it from accepting credentials received during peer authentication. Failing to
		/// raise an exception with such a policy in effect will result in a policy violation because
		/// the caller will consider that it is OK to use the credentials to authenticate the peer.
		/// /// </summary>
		/// <param name="realm">The realm to which the client principal belongs</param>
		/// <param name="principalName">The name of the client principal</param>
		/// <param name="rsaKeys">RSA algorithm with the client principal's public key</param>
		public override void AcceptClientPrincipalCredentials(string realm, string principalName, RSACryptoServiceProvider rsaKeys)
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// Find this contact.
				Identity tempIdentity = localAb.GetIdentityById( principalName );
				if ( tempIdentity == null )
				{
					throw new ApplicationException( "No such identity." );
				}

				// Add the public key to this identity.
				tempIdentity.AddPublicKey( realm, rsaKeys );
				tempIdentity.Commit();
			}
		}

		/// <summary>
		/// Obtains the credentials associated with the specified "client" principal.
		/// </summary>
		/// <param name="realm">The realm to which the client principal belongs</param>
		/// <param name="principalName">The name of the client principal</param>
		/// <param name="rsaKeys">RSA algorithm with the client principal's public key, null if no credentials found</param>
		public override void GetClientPrincipalCredentials(string realm, string principalName, out RSACryptoServiceProvider rsaKeys)
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// Get the public key associated with the domain.
				// If the specified realm is the same as the current realm, use the public key from the 
				// principal name instead of a client key.
				if ( realm == domainName )
				{
					rsaKeys = publicKey;
				}
				else
				{
					// Find the specified contact and return the public key information.
					Identity tempIdentity = localAb.GetIdentityById( principalName );
					if ( tempIdentity != null )
					{
						rsaKeys = tempIdentity.GetDomainPublicKey( realm );
					}
					else
					{
						rsaKeys = null;
					}
				}
			}
		}

		/// <summary>
		/// Obtains the credentials necessary for authentication against the specified server
		/// 
		/// Note: Exception thrown if no credential materials are found.
		/// </summary>
		/// <param name="serverRealm">The realm to which the server principal belongs</param>
		/// <param name="server">The name of the server principal</param>
		/// <param name="realm">The realm to which the client principal belongs</param>
		/// <param name="principalName">The name of the client principal</param>
		/// <param name="rsaKeys">RSA algorithm with the client principal's public and private keys</param>
		public override void GetLocalCredentialsForServer(string serverRealm, string server, out string realm, out string principalName, out RSACryptoServiceProvider rsaKeys)
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// If the server realm is the same as the current realm then use the primary credentials.
				if ( serverRealm != domainName )
				{
					// Find the alias that belongs to the specified domain.
					identity.Refresh();
					Alias alias = identity.GetAliasFromDomain( serverRealm );
					if ( alias == null )
					{
						throw new ApplicationException( "No identity exists for specified domain." );
					}

					principalName = alias.Id;
				}
				else
				{
					principalName = identity.Id;
				}

				realm = domainName;
				rsaKeys = identity.ServerCredential;
			}
		}

		/// <summary>
		/// Gets the server credentials.
		/// 
		/// Note: Exception thrown if no credential materials are found.
		/// </summary>
		/// <param name="realm">The realm to which the principal belongs</param>
		/// <param name="principalName">The name of the server principal</param>
		/// <param name="rsaKeys">RSA algorithm with the principal's public and private keys</param>
		public override void GetServerCredentials(out string realm, out string principalName, out RSACryptoServiceProvider rsaKeys)
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				realm = domainName;
				principalName = identity.Id;
				rsaKeys = identity.ServerCredential;
			}
		}

		/// <summary>
		/// Obtains the credentials associated with the specified "server" principal.
		/// </summary>
		/// <param name="realm">The realm to which the server principal belongs</param>
		/// <param name="principalName">The name of the server principal</param>
		/// <param name="rsaKeys">RSA algorithm with the server principal's public key, null if no credentials found</param>
		public override void GetServerPrincipalCredentials(string realm, string principalName, out RSACryptoServiceProvider rsaKeys)
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// If the realm is the same as the current realm, just return the public key for the current realm.
				if ( realm == domainName )
				{
					rsaKeys = publicKey;
				}
				else
				{
					// Need to look up the alias for the specified domain and return the public key.
					identity.Refresh();
					Alias alias = identity.GetAliasFromDomain( realm );
					if ( alias != null )
					{
						rsaKeys = alias.PublicKey;
					}
					else
					{
						rsaKeys = null;
					}
				}
			}
		}

		/// <summary>
		/// Impersonates the specified identity, if the userId is verified.
		/// </summary>
		/// <param name="userId">User ID to impersonate.</param>
		public void Impersonate( string userId )
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// Look up the specified user in the local address book.
				Identity impersonator = localAb.GetIdentityById( userId );
				if ( impersonator == null )
				{
					throw new ApplicationException( "No such user." );
				}

				// Push the user onto the impersonation stack.
				impersonationId.Push( impersonator );
			}
		}

		/// <summary>
		/// Reverts back to the previous impersonating identity.
		/// </summary>
		public void Revert()
		{
			lock ( store )
			{
				if ( disposed )
				{
					throw new ObjectDisposedException( this.ToString() );
				}

				// Don't ever pop an empty stack.
				if ( impersonationId.Count > 0 )
				{
					impersonationId.Pop();
				}
			}
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Allows for quick release of managed and unmanaged resources.
		/// Called by applications.
		/// </summary>
		public void Dispose()
		{
			lock ( store )
			{
				Dispose( true );
				GC.SuppressFinalize( this );
			}
		}

		/// <summary>
		/// Dispose( bool disposing ) executes in two distinct scenarios.
		/// If disposing equals true, the method has been called directly
		/// or indirectly by a user's code. Managed and unmanaged resources
		/// can be disposed.
		/// If disposing equals false, the method has been called by the 
		/// runtime from inside the finalizer and you should not reference 
		/// other objects. Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">Specifies whether called from the finalizer or from the application.</param>
		private void Dispose( bool disposing )
		{
			// Check to see if Dispose has already been called.
			if ( !disposed )
			{
				// Protect callers from accessing the freed members.
				disposed = true;

				// If disposing equals true, dispose all managed and unmanaged resources.
				if ( disposing )
				{
					// Let go of the address book.
					if ( localAb != null )
					{
						localAb.Dispose();
					}

					// Clean out the impersonation stack.
					impersonationId.Clear();

					// Let go of the other managed references.
					identity = null;
					store = null;
				}
			}
		}
		
		/// <summary>
		/// Use C# destructor syntax for finalization code.
		/// This destructor will run only if the Dispose method does not get called.
		/// It gives your base class the opportunity to finalize.
		/// Do not provide destructors in types derived from this class.
		/// </summary>
		~IdentityManager()      
		{
			lock ( this )
			{
				Dispose( false );
			}
		}
		#endregion
	}
}
