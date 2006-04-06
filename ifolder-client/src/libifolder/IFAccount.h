/***********************************************************************
 *  $RCSfile$
 * 
 *  Copyright (C) 2006 Novell, Inc.
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU General Public
 *  License as published by the Free Software Foundation; either
 *  version 2 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Library General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 *  Author(s): Boyd Timothy <btimothy@novell.com>
 *
 ***********************************************************************/

#ifndef _IFOLDER_ACCOUNT_H_
#define _IFOLDER_ACCOUNT_H_

#include <QObject>
#include <QString>

#include "IFAccount.h"
#include "IFiFolder.h"
#include "IFUser.h"

class iFolderAccount : public QObject
{
	Q_OBJECT
	
	Q_ENUMS(CredentialType)
	Q_ENUMS(SearchProperty)
	Q_ENUMS(SearchOperation)
	
	Q_PROPERTY(QString id READ id)
	Q_PROPERTY(QString name READ name)
	Q_PROPERTY(QString description READ description)
	Q_PROPERTY(QString version READ version)
	Q_PROPERTY(QString hostAddress READ hostAddress)
	Q_PROPERTY(QString machineName READ machineName)
	Q_PROPERTY(QString osVersion READ osVersion)
	Q_PROPERTY(QString userName READ userName)
	Q_PROPERTY(bool isDefault READ isDefault)
	Q_PROPERTY(bool isActive READ isActive)

	public:
		~iFolderAccount();
		
		/**
		 * Enumerations
		 */
		enum CredentialType {Basic, None};
		enum SearchProperty {UserName, FullName, FirstName, LastName};
		enum SearchOperation {BeginsWith, EndsWith, Contains, Equals};
		
		/**
		 * Properties
		 */
		QString id();
		QString name();
		QString description();
		QString version();
		QString hostAddress();
		QString machineName();
		QString osVersion();
		QString userName();
		bool isDefault();
		bool isActive();
		
		/**
		 * User Data
		 */
		void *userData();
		void setUserData(void *userData);
		
		/**
		 * Methods/Actions
		 */
		int remove(bool deleteiFoldersOnServer);
		int login(const char *password);
		int logout();
		int activate();
		int inactivate();
		int changeHostAddress(const char *newHostAddress);
		int setCredentials(const char *password, CredentialType credentialType);
		int setDefault();
		int getAuthenticatedUser(iFolderUser *retVal);
		int getUser(const char *userID, iFolderUser *retVal);
		int getUsers(int index, int count, QList<iFolderUser> *retVal);
		int getUsersBySearch(SearchProperty searchProp, SearchOperation searchOp, const char *pattern, int index, int count, QList<iFolderUser> *retVal);

		int createiFolder(const char *localPath, const char *description, iFolder *retVal);
		int deleteiFolder(iFolder ifolder);
		int getLocaliFolders(int index, int count, QList<iFolder> *retVal);
		int getRemoteiFolders(int index, int count, QList<iFolder> *retVal);
		int getiFolderByID(const char *id, iFolder *retVal);
		int getiFolderByName(const char *name, iFolder *retVal);
		
		/**
		 * Static Methods
		 */
		static int getAll(QList<iFolderAccount> *retVal);
		static int getAllActive(QList<iFolderAccount> *retVal);
		static int getDefault(iFolderAccount *retVal);
		static int add(const char *hostAddress, const char *userName, const char *password, bool makeDefault, iFolderAccount *retVal);
	
	private:
		iFolderAccount(QObject *parent = 0);
};

#endif /*_IFOLDER_ACCOUNT_H_*/