#######################################################################
#  $RCSfile$
#
#  Copyright (C) 2004 Novell, Inc.
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public
#  License as published by the Free Software Foundation; either
#  version 2 of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
#  General Public License for more details.
#
#  You should have received a copy of the GNU General Public
#  License along with this program; if not, write to the Free
#  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
#
#  Author: Calvin Gaisford <cgaisford@novell.com>
#
#######################################################################
EXTRA_DIST = $(srcdir)/mkpkg $(srcdir)/Makefile.am $(srcdir)/client-pkg

PACKAGE_DIR = `pwd`
CLIENT_PKG_DIR = $(PACKAGE_DIR)/client-pkg
IFOLDER_PKG_DIR = $(PACKAGE_DIR)/ifolder-pkg
TMPDIR = $(PACKAGE_DIR)/temp
SIMIAS_PKG = ../../../simias/package/darwin/Simias.pkg

all: $(PACKAGE)-$(VERSION).dmg

iFolder-dist.tmp: 
	cd $(top_srcdir); make dist
	$(mkinstalldirs) $(TMPDIR)
	cd $(TMPDIR); rm -rf $(PACKAGE)-$(VERSION); 
	tar -C $(TMPDIR) -zxvf $(top_srcdir)/$(PACKAGE)-$(VERSION).tar.gz
	cd $(TMPDIR)/$(PACKAGE)-$(VERSION);./configure --prefix=/opt/novell/ifolder3
	cd $(TMPDIR)/$(PACKAGE)-$(VERSION);make;rm -rf $(TMPDIR)/iFolder.dst;
	cd $(TMPDIR)/$(PACKAGE)-$(VERSION);make DESTDIR=$(TMPDIR)/iFolder.dst install
	touch iFolder-dist.tmp

iFolderClient.pkg: iFolder-dist.tmp
	./mkpkg -build -p $(PACKAGE_DIR)/iFolderClient.pkg -f $(TMPDIR)/$(PACKAGE)-$(VERSION)/temp/iFolder.dst/opt/novell/ifolder3 -r $(CLIENT_PKG_DIR)/Install_Resources -i $(CLIENT_PKG_DIR)/Info.plist -d $(CLIENT_PKG_DIR)/Description.plist


$(PACKAGE)-$(VERSION).dmg: iFolderClient.pkg $(SIMIAS_PKG)
	rm -rf $(PACKAGE)-$(VERSION)
	$(mkinstalldirs) $(PACKAGE)-$(VERSION)/.Packages
	cp -r iFolderClient.pkg $(PACKAGE)-$(VERSION)/.Packages
	cp -r $(SIMIAS_PKG) $(PACKAGE)-$(VERSION)/.Packages
	tar -C $(PACKAGE)-$(VERSION) -xvf iFolder.tar
	hdiutil create -srcfolder $(PACKAGE)-$(VERSION) $(PACKAGE)-$(VERSION).dmg

clean-local:
	rm -rf *.pkg
	rm -rf *.dmg
	rm -rf $(TMPDIR)
	rm -rf iFolder-dist.tmp
	rm -rf $(PACKAGE)-$(VERSION)

distclean-local:

maintainer-clean-local:
	rm -rf $(COMMON_MAINTAINER_CLEAN_FILES)

