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

#include "ifolder-errors.h"
#include "libipcserver/IFIPCServer.h"
#include "IFiFolderClient.h"

iFolderClient::iFolderClient() :
	bInitialized(false), ipcClass(NULL)
{
}

iFolderClient::~iFolderClient()
{
	int err;
	iFolderIPCServer *ipcServer;

	if (ipcClass != NULL)
	{
		ipcServer = (iFolderIPCServer *)ipcClass;
		if (ipcServer->isRunning())
		{
			ipcServer->gracefullyExit();
			if (ipcServer->wait(5000) != true)
			{
				// KILL the thread
				ipcServer->exit(-1);
			}
		}
		
		delete ipcServer;
		ipcClass = NULL;
	}
}

int
iFolderClient::initialize()
{
	int err;
	iFolderIPCServer *ipcServer;

	if (bInitialized)
		return IFOLDER_ERROR_ALREADY_INITIALIZED;

	// FIXME: Initialize the client (i.e., start up the IPC server, etc.)
	ipcServer = new iFolderIPCServer(/*NULL*/);
	if (!ipcServer)
		return IFOLDER_ERROR_OUT_OF_MEMORY;

	ipcClass = ipcServer; // Hold onto this pointer

//	connect(ipcServer, SIGNAL(finished()), ipcServer, SLOT(deleteLater()));
	ipcServer->start();

	bInitialized = true;
	return IFOLDER_SUCCESS;
}

int
iFolderClient::uninitialize()
{
	int err;
	iFolderIPCServer *ipcServer;

	if (!bInitialized)
		return IFOLDER_ERROR_NOT_INITIALIZED;

	// FIXME: Uninitialize the client (i.e., stop the IPC server, etc.)
	ipcServer = (iFolderIPCServer *)ipcClass;
	if (ipcServer->isRunning())
	{
		ipcServer->gracefullyExit();
		if (ipcServer->wait(5000) != true)
		{
			// KILL the thread
			ipcServer->exit(-1);
		}
	}
	
	delete ipcServer;
	ipcClass = NULL;

	bInitialized = false;
	return IFOLDER_SUCCESS;
}

int
iFolderClient::startTrayApp(QString trayAppExePath)
{
	return IFOLDER_SUCCESS;
}
