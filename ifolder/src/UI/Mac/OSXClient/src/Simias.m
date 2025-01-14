/*****************************************************************************
*
* Copyright (c) [2009] Novell, Inc.
* All Rights Reserved.
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of version 2 of the GNU General Public License as
* published by the Free Software Foundation.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.   See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, contact Novell, Inc.
*
* To contact Novell about this file by physical or electronic mail,
* you may find current contact information at www.novell.com
*
*-----------------------------------------------------------------------------
*
*                 $Author: Calvin Gaisford <cgaisford@novell.com>
*                 $Modified by: <Modifier>
*                 $Mod Date: <Date Modified>
*                 $Revision: 0.0
*-----------------------------------------------------------------------------
* This module is used to:
*        <Description of the functionality of the file >
*
*
*******************************************************************************/

#import "Simias.h"
#import "config.h"

#import <netinet/in.h>
#include <arpa/inet.h>
#include <errno.h>
#include <sys/sysctl.h>
#include <signal.h>
#include "applog.h"
#include "config.h"


@implementation Simias

static Simias *sharedInstance = nil;

//====================================================================
// init
// Initiate the simias process
//====================================================================
- (id)init 
{
    if (sharedInstance) 
	{
        [self dealloc];
    } 
	else
	{
        sharedInstance = [super init];
    }
	
	simiasManager = AllocateManager();
	SetShowConsole(simiasManager, 1);
	SetVerbose(simiasManager, 1);
	// If simiasManager couldn't find the simias path,
	// set it to the default SIMIAS_PATH that we built against
	if(GetApplicationPath(simiasManager) == NULL)
		SetApplicationPath(simiasManager, DEFAULT_SIMIAS_PATH);
    return sharedInstance;
}

//====================================================================
// dealloc
// Deallocate the resources allocated
//====================================================================
-(void) dealloc
{
	FreeManager(simiasManager);
	[super dealloc];
}


//====================================================================
// getInstance
// Get the instaance of simias process
//====================================================================
+ (Simias *)getInstance
{
    return sharedInstance ? sharedInstance : [[self alloc] init];
}


//====================================================================
// simiasURL
// Get the Web service URL associated with simias manager
//====================================================================
-(NSString *)simiasURL
{
	const char *url = GetWebServiceUrl(simiasManager);
	if(url == NULL)
		return @"";
	else
		return [NSString stringWithCString:url];
}



//Starts the simias process
//====================================================================
// start
// Start the simias Process
//====================================================================
-(BOOL)start
{
	if( Start(simiasManager) !=  NULL )
	{
		return YES;
	}
	else
		return NO;
}



//Stops the simias process
//====================================================================
// stop
// Stop the simias process
//====================================================================
-(BOOL)stop
{
	if(Stop(simiasManager))
		return YES;
	else
		return NO;
}



@end
