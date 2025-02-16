# handle Mono secondary dependencies
export MONO_PATH := $(MONO_PATH)

iFolderWebService_CSFILES = $(srcdir)/iFolderService.cs $(srcdir)/iFolder.cs $(srcdir)/DiskSpace.cs $(srcdir)/iFolderUser.cs $(srcdir)/Conflict.cs $(srcdir)/SyncSize.cs $(srcdir)/ClientUpgrade.cs $(srcdir)/ClientUpdate.cs $(srcdir)/GenerateWsdl.cs
iFolderWebService_CSFILES_CSC := $(subst /,$(SEP),$(iFolderWebService_CSFILES))
iFolderWebService_INCLUDES =
iFolderWebService_RESOURCES =
iFolderWebService_FLAGS = $(CSC_LIBFLAG)
iFolderWebService_LIBS = System.dll System.Data.dll $(SYSTEM_XML) System.Web.dll System.Web.Services.dll SimiasClient.dll
iFolderWebService_LIBPATH = $(LOG4NET_DIR) $(webbindir)
GenerateWsdl_LIBS = System.dll System.Data.dll $(SYSTEM_XML) System.Web.dll System.Web.Services.dll
Simias_WEBBINPATH = `pkg-config --variable=libdir simias-client`

EXTRA_DIST = iFolder.asmx $(iFolderWebService_CSFILES) AssemblyInfo.cs.in

if MAKE_DOC_FILE
DOC_FILE = $(basename $@).doc.xml
DOC_FILE_CMD = /doc:$(DOC_FILE)
DOC_FILE_CLEAN = *.doc.xml
endif

all: Novell.iFolder.Web.dll ../iFolder/iFolderWebService.cs

Novell.iFolder.Web.dll: $(srcdir)/AssemblyInfo.cs $(iFolderWebService_CSFILES) $(iFolderWebService_RESOURCES)
	$(CSC) /out:$@ $(CSCFLAGS) $(iFolderWebService_FLAGS) $(SIMIAS_LIBS) $(SIMIAS_CLIENT_LIBS) $(iFolderWebService_LIBS:%=/r:%) $(iFolderWebService_LIBPATH:%=/lib:%) $(iFolderWebService_RESOURCES:%=$(ICON_FLAG)%) $(srcdir)/AssemblyInfo.cs $(iFolderWebService_CSFILES_CSC) $(DOC_FILE_CMD) 

if LINUX
../iFolder/iFolderWebService.cs: iFolderWebService.wsdl
	$(WSDL) -out:../iFolder/iFolderWebService.cs iFolderWebService.wsdl
endif
if DARWIN
../iFolder/iFolderWebService.cs: iFolderWebService.wsdl
        wsdl -out:../iFolder/iFolderWebService.cs iFolderWebService.wsdl
endif

iFolderWebService.wsdl: Novell.iFolder.Web.dll GenerateWsdl.exe copy
	$(MONO) GenerateWsdl.exe Novell.iFolder.Web.dll Novell.iFolder.Web.iFolderService http://127.0.0.1:8086/simias10/iFolder.asmx $@
	
GenerateWsdl.exe: GenerateWsdl.cs
	$(CSC) /out:$@ $(CSCFLAGS) $(CSC_EXEFLAG) $(GenerateWsdl_LIBS:%=/r:%) $<	
copy:
	cp $(Simias_WEBBINPATH)/SimiasClient.dll .
	cp $(Simias_WEBBINPATH)/SimiasLib.dll .

if DEBUG
if WINDOWS
DEBUG_FILES = Novell.iFolder.Web.pdb
endif
endif

install-exec-local: all $(DATA_FILES)
	$(mkinstalldirs) $(DESTDIR)$(webdir)
	$(mkinstalldirs) $(DESTDIR)$(webbindir)
	$(INSTALL_PROGRAM) iFolder.asmx $(DEBUG_FILES) $(DESTDIR)$(webdir)
	$(INSTALL_PROGRAM) Novell.iFolder.Web.dll $(DEBUG_FILES) $(DESTDIR)$(webbindir)
	
uninstall-local:
	cd $(DESTDIR)$(webdir); rm -f iFolder.asmx $(DEBUG_FILES)
	cd $(DESTDIR)$(webbindir); rm -f Novell.iFolder.Web.dll $(DEBUG_FILES)

installcheck-local: install
	
CLEAN_FILES = Novell.iFolder.Web.dll $(DOC_FILE_CLEAN) AssemblyInfo.cs ../iFolder/iFolderWebService.cs iFolderWebService.wsdl

clean-local:
	rm -rf $(CLEAN_FILES) $(COMMON_CLEAN_FILES)

distclean-local:
	rm -rf $(COMMON_DISTCLEAN_FILES)
	rm -rf AssemblyInfo.cs
	rm -rf Makefile

maintainer-clean-local:
	rm -rf $(COMMON_MAINTAINER_CLEAN_FILES)
	rm -rf AssemblyInfo.cs


