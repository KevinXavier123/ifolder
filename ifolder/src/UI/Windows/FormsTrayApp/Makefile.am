# handle Mono secondary dependencies
export MONO_PATH := $(MONO_PATH)

if WINDOWS
INSTALLUTIL := $(shell cygpath --unix "$(FRAMEWORKDIR)\v1.1*\InstallUtil.exe")
endif

iFolderApp_CSFILES = $(srcdir)/FormsTrayApp.cs $(srcdir)/GlobalProperties.cs $(srcdir)/iFolderAppInstallerClass.cs $(srcdir)/ServerInfo.cs $(srcdir)/ShellNotifyIcon.cs $(srcdir)/BalloonType.cs $(srcdir)/iFolderMigration.cs $(srcdir)/DomainConnectEventArgs.cs $(srcdir)/DomainRemoveEventArgs.cs $(srcdir)/ServerDetails.cs $(srcdir)/SyncLog.cs $(srcdir)/Preferences.cs $(srcdir)/AdvancedSettings.cs $(srcdir)/Domain.cs $(srcdir)/iFolderObject.cs $(srcdir)/Security.cs $(srcdir)/About.cs $(srcdir)/Credits.cs $(srcdir)/ToolBarEx.cs $(srcdir)/RemoveAccount.cs $(srcdir)/Connecting.cs $(srcdir)/AccountWizard/ServerPage.cs $(srcdir)/AccountWizard/BaseWizardPage.cs $(srcdir)/AccountWizard/CompletionPage.cs $(srcdir)/AccountWizard/InteriorPageTemplate.cs $(srcdir)/AccountWizard/AccountWizard.cs $(srcdir)/AccountWizard/VerifyPage.cs $(srcdir)/AccountWizard/IdentityPage.cs $(srcdir)/AccountWizard/WelcomePage.cs $(srcdir)/AccountWizard/WelcomePageTemplate.cs $(srcdir)/MigrationWizard/MigrationWizard.cs $(srcdir)/MigrationWizard/MigrationWelcomePage.cs $(srcdir)/MigrationWizard/MigrationWelcomePageTemplate.cs $(srcdir)/MigrationWizard/MigrationBaseWizardPage.cs $(srcdir)/MigrationWizard/MigrationServerPage.cs $(srcdir)/MigrationWizard/MigrationInteriorPageTemplate.cs $(srcdir)/MigrationWizard/MigrationVerifyPage.cs $(srcdir)/MigrationWizard/MigrationIdentityPage.cs $(srcdir)/MigrationWizard/MigrationCompletionPage.cs $(srcdir)/iFoldersListView.cs $(srcdir)/ItemSelectedEventArgs.cs $(srcdir)/NoiFolderMessage.cs $(srcdir)/TileListView.cs $(srcdir)/TileListViewItem.cs $(srcdir)/TileListViewItemCollection.cs $(srcdir)/RevertiFolder.cs $(srcdir)/NavigateItemEventArgs.cs $(srcdir)/MigrationWindow.cs $(srcdir)/ResetPassPhrase.cs $(srcdir)/ImportKeysDialog.cs $(srcdir)/ExportKeysDialog.cs $(srcdir)/EnterPassphraseDialog.cs $(srcdir)/VerifyPassphraseDialog.cs $(srcdir)/MigrationWizard/MigrationPassphrasePage.cs $(srcdir)/MigrationWizard/MigrationPassphraseVerifyPage.cs $(srcdir)/AccountWizard/DefaultiFolderPage.cs
iFolderApp_CSFILES_CSC := $(subst /,$(SEP),$(iFolderApp_CSFILES))
iFolderApp_INCLUDES =
iFolderApp_RESOURCES = $(srcdir)/App$(ICON_EXT)
iFolderApp_RESX_FILES := $(wildcard *.resx) 
iFolderApp_LANGUAGES := $(subst .,,$(sort $(suffix $(basename $(iFolderApp_RESX_FILES)))))
iFolderApp_RESOURCES_DLLS := $(iFolderApp_LANGUAGES:%=$(srcdir)/%/iFolderApp.resources.dll)
iFolderApp_RESOURCES_FILES = Novell.FormsTrayApp.FormsTrayApp.resources Novell.FormsTrayApp.GlobalProperties.resources Novell.FormsTrayApp.ServerInfo.resources Novell.FormsTrayApp.ServerDetails.resources Novell.FormsTrayApp.SyncLog.resources Novell.FormsTrayApp.Preferences.resources Novell.FormsTrayApp.AdvancedSettings.resources Novell.FormsTrayApp.About.resources Novell.FormsTrayApp.Credits.resources Novell.FormsTrayApp.RemoveAccount.resources Novell.FormsTrayApp.Connecting.resources Novell.FormsTrayApp.NoiFolderMessage.resources Novell.FormsTrayApp.RevertiFolder.resources Novell.FormsTrayApp.ResetPassphrase.resources Novell.FormsTrayApp.EnterPassphraseDialog.resources Novell.FormsTrayApp.VerifyPassphraseDialog.resources Novell.FormsTrayApp.ExportKeysDialog.resources Novell.FormsTrayApp.ImportKeysDialog.resources Novell.FormsTrayApp.iFoldersListView.resources Novell.FormsTrayApp.TileListView.resources Novell.FormsTrayApp.TileListViewItem.resources Novell.FormsTrayApp.MigrationWindow.resources
iFolderApp_FLAGS = $(CSC_WINEXEFLAG)
iFolderApp_LIBS = System.dll System.Data.dll System.Drawing.dll System.Windows.Forms.dll $(SYSTEM_XML) Novell.iFolder.dll iFolderComponent.dll SimiasClient.dll
iFolderApp_LIBPATH = ../iFolderShell ../iFolder

DATA_FILES = App.ico ifolder_app.ico
RES_FILES = res/ifolder_loaded.ico res/ifolder_sync1.ico res/ifolder_sync2.ico res/ifolder_sync3.ico res/ifolder_sync4.ico res/ifolder_sync5.ico res/ifolder_sync6.ico res/ifolder_sync7.ico res/ifolder_sync8.ico res/ifolder_sync9.ico res/ifolder_sync10.ico $(srcdir)/../images/ifolder-banner.png $(srcdir)/../images/ifolder-banner-scaler.png res/ifolder_contact_card.ico res/inviteduser.ico res/ifolder_me_card.ico res/serverifolder.ico res/ifolderconflict.ico res/ifolder-startup.ico res/ifolder-shutdown.ico res/mtoolbar_nor.bmp res/mtoolbar_dis.bmp res/mtoolbar_hot.bmp res/ltoolbar_nor.bmp res/ltoolbar_dis.bmp res/ltoolbar_hot.bmp res/ifolder-download48.png res/ifolder-error48.png res/ifolder-add-account48.png res/ifolder-sync48.png res/ifolder-upload48.png res/ifolder-waiting48.png res/ifolder-warning48.png res/ifolder48.png res/ifolder_16.ico res/ifolder_upload_16.ico res/ifolder_waiting_16.ico res/ifolder_download_16.ico res/ifolder_startup_nl.gif res/encrypt_ilock_48.gif res/ifolder_user_48.png res/ifolder_account_wiz.png

PROJECT_FILES = $(srcdir)/FormsTrayApp.csproj $(srcdir)/FormsTrayApp.sln

EXTRA_DIST = $(iFolderApp_CSFILES) $(DATA_FILES) $(RES_FILES) $(iFolderApp_RESX_FILES) iFolderApp.exe.manifest iFolderApp.exe.config InstallUtil.exe.config $(PROJECT_FILES) AssemblyInfo.cs.in iFolderUpgrade.exe

if MAKE_DOC_FILE
DOC_FILE = $(basename $@).doc.xml
DOC_FILE_CMD = /doc:$(DOC_FILE)
DOC_FILE_CLEAN = *.doc.xml
endif

all: iFolderApp.exe $(iFolderApp_RESOURCES_DLLS)
	
iFolderApp.exe: $(srcdir)/AssemblyInfo.cs $(iFolderApp_RESOURCES_FILES) $(iFolderApp_CSFILES) $(iFolderApp_RESOURCES)
	$(CSC) /out:$@ $(CSCFLAGS) $(iFolderApp_FLAGS) $(SIMIAS_CLIENT_LIBS) $(iFolderApp_LIBS:%=/r:%) $(iFolderApp_LIBPATH:%=/lib:%) $(iFolderApp_RESOURCES:%=$(ICON_FLAG)%) $(iFolderApp_RESOURCES_FILES:%=/res:%) $(srcdir)/AssemblyInfo.cs $(iFolderApp_CSFILES_CSC) $(DOC_FILE_CMD)

Novell.FormsTrayApp.%.resources : %.resx
	$(RESG) $< $@
	
%/iFolderApp.resources.dll: Novell.FormsTrayApp.FormsTrayApp.%.resources Novell.FormsTrayApp.GlobalProperties.%.resources Novell.FormsTrayApp.ServerInfo.%.resources Novell.FormsTrayApp.ServerDetails.%.resources Novell.FormsTrayApp.SyncLog.%.resources Novell.FormsTrayApp.Preferences.%.resources Novell.FormsTrayApp.AdvancedSettings.%.resources Novell.FormsTrayApp.About.%.resources Novell.FormsTrayApp.Credits.%.resources Novell.FormsTrayApp.RemoveAccount.%.resources Novell.FormsTrayApp.Connecting.%.resources Novell.FormsTrayApp.NoiFolderMessage.%.resources Novell.FormsTrayApp.RevertiFolder.%.resources
	$(mkinstalldirs) $*
	al /nologo /out:$@ /c:$* /template:iFolderApp.exe $(patsubst %,/embed:%,$^)


if DEBUG
DEBUG_FILES = iFolderApp.pdb
endif

install-util:
	if ! test -f "$(DESTDIR)$(bindir)/iFolderApp.exe"; then \
		echo "run 'make install' before 'make install-util'"; \
		exit 1; \
	fi
	$(INSTALLUTIL) /LogToConsole=false /ShowCallStack /LogFile=iFolderApp.InstallLog `cygpath --windows "$(DESTDIR)$(bindir)/iFolderApp.exe"`

uninstall-util:
	if test -f "$(DESTDIR)$(bindir)/iFolderApp.exe"; then \
		$(INSTALLUTIL) /uninstall /LogToConsole=false /ShowCallStack /LogFile=iFolderApp.UninstallLog `cygpath --windows "$(DESTDIR)$(bindir)/iFolderApp.exe"`; \
	fi

install-exec-local: all $(DATA_FILES)
	$(mkinstalldirs) $(DESTDIR)$(bindir)
	$(mkinstalldirs) $(DESTDIR)$(bindir)/res
	$(INSTALL_PROGRAM) iFolderApp.exe iFolderApp.exe.manifest iFolderApp.exe.config iFolderUpgrade.exe $(DEBUG_FILES) $(DESTDIR)$(bindir)
	$(INSTALL_PROGRAM) $(DATA_FILES) $(DESTDIR)$(bindir)
	$(INSTALL_PROGRAM) $(RES_FILES) $(DESTDIR)$(bindir)/res
	$(mkinstalldirs) $(iFolderApp_LANGUAGES:%=$(DESTDIR)$(bindir)/%)
	cp -r $(iFolderApp_LANGUAGES) $(DESTDIR)$(bindir)
	find $(DESTDIR)$(bindir) -name ".svn" | xargs rm -rf
if DEBUG	
	$(INSTALL_PROGRAM) $(PROJECT_FILES) $(DESTDIR)$(bindir)
endif

uninstall-local:
	cd $(DESTDIR)$(bindir); rm -f iFolderApp.exe iFolderApp.exe.manifest iFolderApp.exe.config iFolderUpgrade.exe $(DEBUG_FILES)
	cd $(DESTDIR)$(bindir); rm -f $(DATA_FILES)
	cd $(DESTDIR)$(bindir)/res; rm -f $(RES_FILES:res/%=%) ifolder-banner.png ifolder-banner-scaler.png
	cd $(DESTDIR)$(bindir); rm -f $(iFolderApp_RESOURCES_DLLS)
if DEBUG	
	cd $(DESTDIR)$(bindir); rm -f $(PROJECT_FILES:$(srcdir)/%=%)
endif

CLEAN_FILES = $(iFolderApp_LANGUAGES) iFolderApp.exe $(DOC_FILE_CLEAN) $(iFolderApp_RESX_FILES:%.resx=Novell.FormsTrayApp.%.resources) AssemblyInfo.cs

clean-local:
	rm -rf $(CLEAN_FILES) $(filter-out *.exe,$(COMMON_CLEAN_FILES))
	rm -f *Log

distclean-local:
	rm -rf $(filter-out *.exe,$(COMMON_DISTCLEAN_FILES))
	rm -rf AssemblyInfo.cs
	rm -rf Makefile

maintainer-clean-local:
	rm -rf $(filter-out *.exe,$(COMMON_MAINTAINER_CLEAN_FILES))
	rm -rf AssemblyInfo.cs

