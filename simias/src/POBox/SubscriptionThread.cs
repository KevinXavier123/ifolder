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
 *  Author: Rob
 *
 ***********************************************************************/

using System;
using System.Collections;
using System.Threading;
using System.Text;
using System.IO;
using System.Runtime.Remoting;

using Simias;
using Simias.Storage;
using Simias.Sync;
using Simias.Mail;

namespace Simias.POBox
{
	/// <summary>
	/// SubscriptionThread
	/// </summary>
	public class SubscriptionThread
	{
		private static readonly ISimiasLog log = SimiasLogManager.GetLogger(typeof(SubscriptionThread));
		private static readonly string	poServiceLabel = "/POBoxService.asmx";
		
		private POBox poBox;
		private Subscription subscription;
		private Hashtable threads;
		private string poServiceUrl;

		/// <summary>
		/// Constructor
		/// </summary>
		public SubscriptionThread(POBox poBox, Subscription subscription, Hashtable threads)
		{
			this.poBox = poBox;
			this.subscription = subscription;
			this.threads = threads;

			if (subscription.SubscriptionCollectionURL != null && 
				subscription.SubscriptionCollectionURL != "")
			{
				this.poServiceUrl = subscription.SubscriptionCollectionURL + poServiceLabel;
			}
			else
			{
				Collection cCollection = 
					poBox.StoreReference.GetCollectionByID(subscription.SubscriptionCollectionID); 
				if (cCollection == null)
				{
					throw new ApplicationException("Invalid shared collection ID");
				}

				SyncCollection sc = new SyncCollection(cCollection);
				poServiceUrl = sc.MasterUrl.ToString() + poServiceLabel;
			}
		}

		/// <summary>
		/// Thread Run
		/// </summary>
		public void Run()
		{
			bool done = false;

			try
			{
				while(!done)
				{
					try
					{
						switch(subscription.SubscriptionState)
						{
							// invited (master)
							case SubscriptionStates.Invited:
								done = DoInvited();
								break;

							// TODO: fix and cleanup states
							//case SubscriptionStates.Pending:

							// replied (slave)
							case SubscriptionStates.Replied:
								done = DoReplied();
								break;

							// delivered (slave)
							case SubscriptionStates.Delivered:
								done = DoDelivered();
								break;

							default:
								break;
						}
					}
					catch(Exception e)
					{
						done = false;
						log.Debug(e, "Ignored");
					}

					if (!done)
					{
						Thread.Sleep(TimeSpan.FromSeconds(10));
					}
				}
			}
			catch(Exception e)
			{
				log.Debug(e, "Ignored");
			}
			finally
			{
				lock(threads.SyncRoot)
				{
					threads.Remove(subscription.ID);
				}
			}
		}

		private bool DoInvited()
		{
			bool returnStatus = true;
			if (poBox.Domain == Simias.Storage.Domain.WorkGroupDomainID)
			{
				// TODO: Localize
			
				MailMessage message = new MailMessage();

				message.BodyFormat = MailFormat.Text;

				message.From = subscription.FromAddress;
				message.To = subscription.ToAddress;

				message.Subject =
					String.Format("Shared iFolder Invitation - {0}", subscription.SubscriptionCollectionName);
			
				// body
				StringBuilder buffer = new StringBuilder();
			
				buffer.AppendFormat("{0} invites you to participate in the shared iFolder named \"{1}\".\n",
					subscription.FromName, subscription.SubscriptionCollectionName);

				// TODO: Owner currently cannot be resolved to a friendly name.  Also need to add "Novell iFolder server" for enterprise.
				// buffer.AppendFormat("This iFolder is hosted by {0} on a personal computer.\n\n", invitation.Owner);
				buffer.Append("\n");

				if (subscription.CollectionDescription != null)
				{
					buffer.AppendFormat("{0}\n\n", subscription.CollectionDescription);
				}

				string rights;
				switch (subscription.SubscriptionRights)
				{
					case Storage.Access.Rights.Admin:
						rights = "Full Control";
						break;
					case Storage.Access.Rights.ReadWrite:
						rights = "Read/Write";
						break;
					default:
						rights = "Read Only";
						break;
				}

				buffer.AppendFormat("{0} assigned you {1} rights to this shared iFolder.\n\n",
					subscription.FromName, rights);

				buffer.Append("You can participate from one or more computers with the iFolder client. For information or download, see the iFolder Web site at http://www.ifolder.com. \n\n");

				buffer.Append("To accept and set up the shared iFolder on this computer, open the attached Collection Subscription Information (CSI) file. Repeat this process on each computer where you want to set up the shared iFolder.\n\n");

				buffer.Append("If you do not accept within 7 days, the invitation  automatically expires. To decline immediately, open the CSI file and select Decline.");

				message.Body = buffer.ToString();

				// invitation attachment
				string filename = Path.Combine(Path.GetTempPath(),
					subscription.SubscriptionCollectionName + "_" + subscription.FromName
					+ SubscriptionInfo.Extension);

				SubscriptionInfo info = subscription.GenerateInfo(poBox.StoreReference);
				info.Save(filename);

				MailAttachment attachment = new MailAttachment(filename);
			
				message.Attachments.Add(attachment);

				// send
				SmtpMail.Send(message);

				// delete invitation file
				File.Delete(filename);

				log.Info("Invitation Sent: {0}", info);

				// update subscription
				subscription.SubscriptionState = SubscriptionStates.Posted;
				poBox.Commit(subscription);
			}
			else
			{
				log.Info("SubscriptionThread::DoInvited called");
				// Make sure the local (from) subscription and the shared collection
				// have sync'd to the server before inviting

				SubscriptionInformation subInfo = null;
				POBoxService poService = new POBoxService();
				poService.Url = this.poServiceUrl;

				try
				{
					subInfo =
						poService.GetSubscriptionInfo(
							subscription.DomainID,
							subscription.FromIdentity,
							subscription.MessageID);
				}
				catch{}

				if (subInfo != null)
//				if (subscription.LocalIncarnation == subscription.MasterIncarnation)
				{
					// This is an enterprise pobox contact the POService.
					log.Debug("Connecting to the Post Office Service : {0}", subscription.POServiceURL);

					try
					{
						// Set the remote state to received.
						// And post the subscription to the server.
						Simias.Storage.Member me = poBox.GetCurrentMember();
						subscription.FromIdentity = me.UserID;
						subscription.FromName = me.Name;

						string subID =
							poService.Invite(
								subscription.DomainID,
								subscription.FromIdentity,
								subscription.ToIdentity,
								subscription.SubscriptionCollectionID,
								subscription.SubscriptionCollectionType,
								(int) subscription.SubscriptionRights);
						if (subID != null && subID != "")
						{
							subscription.SubscriptionState = SubscriptionStates.Posted;
							subscription.MessageID = subID;
							poBox.Commit(subscription);
						}
						else
						{
							returnStatus = false;
						}
					}
					catch
					{
						log.Debug("Failed POBoxService::Invite - target: " + poService.Url);
						returnStatus = false;
					}
				}
				else
				{
					returnStatus = false;
				}
			}

			return returnStatus;
		}

		private bool DoReplied()
		{
			log.Info("DoReplied - Connecting to the Post Office Service : {0}", subscription.POServiceURL);
			bool status = false;
			POBoxService poService = new POBoxService();
			poService.Url = this.poServiceUrl;
			POBoxStatus	wsStatus = POBoxStatus.UnknownError;

			try
			{
				if (subscription.SubscriptionDisposition == SubscriptionDispositions.Accepted)
				{
					log.Info("  subscription accepted!");
					wsStatus =
						poService.AcceptedSubscription(
							subscription.DomainID,
							subscription.FromIdentity,
							subscription.ToIdentity,
							subscription.MessageID);
				}
				else
				if (subscription.SubscriptionDisposition == SubscriptionDispositions.Declined)
				{
					log.Info("  subscription declined");
					wsStatus =
						poService.DeclinedSubscription(
							subscription.DomainID,
							subscription.FromIdentity,
							subscription.ToIdentity,
							subscription.MessageID);
				}

				// update local subscription
				if (wsStatus == POBoxStatus.Success)
				{
					subscription.SubscriptionState = SubscriptionStates.Delivered;
					poBox.Commit(subscription);
				}
				else
				{
					poBox.Commit(poBox.Delete(subscription));

					log.Debug(
						"Failed Accepting/Declining a subscription.  Status: " + 
						wsStatus.ToString());

					// return true so the thread controlling the
					// subscription will die off
					status = true;
				}
			}
			catch(Exception e)
			{
				log.Debug("DoReplied failed updating originator's PO box");
				log.Debug(e.Message);
				log.Debug(e.StackTrace);
			}
			poService = null;

			// always return false to drop to the next state
			return status;
		}

		private bool DoDelivered()
		{
			bool result = false;

			log.Info("DoDelivered::Connecting to the Post Office Service : {0}", this.poServiceUrl);

			POBoxService poService = new POBoxService();
			poService.Url = this.poServiceUrl;

			try
			{
				log.Info("  calling the PO Box server to get subscription state");
				log.Info("  domainID: " + subscription.DomainID);
				log.Info("  fromID:   " + subscription.FromIdentity);
				log.Info("  SubID:    " + subscription.MessageID);

				SubscriptionInformation subInfo =
					poService.GetSubscriptionInfo(
						subscription.DomainID,
						subscription.FromIdentity,
						subscription.MessageID);

				if (subInfo != null)
				{
					log.Info("  subInfo.FromName: " + subInfo.FromName);
					log.Info("  subInfo.ToName: " + subInfo.ToName);
					log.Info("  subInfo.State: " + subInfo.State.ToString());
					log.Info("  subInfo.Disposition: " + subInfo.Disposition.ToString());
				}

				// update subscription
				if (subInfo.State == (int) SubscriptionStates.Responded)
				{
					// create proxy
					if (subInfo.Disposition == (int) SubscriptionDispositions.Accepted)
					{
						log.Debug("Creating collection...");

						// do not re-create the proxy
						if (poBox.StoreReference.GetCollectionByID(subscription.SubscriptionCollectionID) == null)
						{
							SubscriptionDetails details = new SubscriptionDetails();
							details.DirNodeID = subInfo.DirNodeID;
							details.DirNodeName = subInfo.DirNodeName;
							details.CollectionUrl = subInfo.CollectionUrl;

							log.Debug("Collection URL: " + subInfo.CollectionUrl);

							subscription.SubscriptionRights = 
								(Simias.Storage.Access.Rights) subInfo.AccessRights;

							// save details
							subscription.AddDetails(details);
							poBox.Commit(subscription);
					
							// create slave stub
							subscription.CreateSlave(poBox.StoreReference);
						}

						// acknowledge the message
						// which removes the originator's 
						POBoxStatus wsStatus =
							poService.AckSubscription(
								subscription.DomainID,
								subscription.FromIdentity, 
								subscription.ToIdentity,
								subscription.MessageID);

						if (wsStatus == POBoxStatus.Success)
						{
							// done with the subscription - move to local subscription to the ready state
							subscription.SubscriptionState = SubscriptionStates.Ready;
							poBox.Commit(subscription);
						}
						else
						{
							log.Debug(
								"Failed Acking a subscription.  Status: " + 
								wsStatus.ToString());

							poBox.Commit(poBox.Delete(subscription));
						}
					}
					else
					{
						// Remove the subscription from the local PO box
						poBox.Commit(poBox.Delete(subscription));
					}

					// done
					result = true;
				}
			}
			catch(Exception e)
			{
				log.Debug("SubscriptionThread::DoDelivered failed with an exception");
				log.Debug(e.Message);
				log.Debug(e.StackTrace);
			}
			return result;
		}
	}
}
