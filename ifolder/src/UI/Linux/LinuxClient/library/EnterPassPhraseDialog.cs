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
  *                 $Author: Ramesh Sunder <sramesh@novell.com> 
  *                 $Modified by: <Modifier>
  *                 $Mod Date: <Date Modified>
  *                 $Revision: 0.0
  *-----------------------------------------------------------------------------
  * This module is used to:
  *        <Description of the functionality of the file >
  *
  *
  *******************************************************************************/

using Gtk;
using System;
using Novell.iFolder.Controller;

namespace Novell.iFolder
{
    /// <summary>
    /// class Enter Passphrase Dialog
    /// </summary>
	public class EnterPassPhraseDialog : Dialog
	{
		private Entry PassPhraseEntry;
		private Entry           PassPhraseVerifyEntry;
		private CheckButton savePassPhraseButton;
		private string[] RAList;
		private string	DomainID;

		ListStore RATreeStore;
		private iFolderTreeView RATreeView;
		private Image				 iFolderBanner;
		private Image				 iFolderScaledBanner;
		private Gdk.Pixbuf			 ScaledPixbuf;
        	private SimiasWebService simws;

        /// <summary>
        /// Gets he Passphrase
        /// </summary>
		public string PassPhrase
		{
			get
			{
				return PassPhraseEntry.Text;
			}
		}

        /// <summary>
        /// Gets the Retyped Passphrase
        /// </summary>
		public string RetypedPassPhrase
		{
			get
			{
				return PassPhraseVerifyEntry.Text;
			}
		}	

        /// <summary>
        /// Gets the Save Pass Phrase status
        /// </summary>
		public bool ShouldSavePassPhrase
		{
			get
			{
				return savePassPhraseButton.Active;
			}
		}

        /// <summary>
        /// Gets the Recovery Agent
        /// </summary>
		public string RecoveryAgent
		{
			get
			{
				TreeSelection tSelect = RATreeView.Selection;
				if(tSelect == null)
					return null;
				if(tSelect.CountSelectedRows() == 1)
				{
					TreeModel tModel;
					TreeIter iter;
					tSelect.GetSelected(out tModel, out iter);
					string id = (string) tModel.GetValue(iter, 0);
					if(id == Util.GS("None"))
						return null;
					return id;
				}
				return null; 
			}
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="domainID">Domain ID</param>
        /// <param name="simiasws">Simias WebService</param>
		public EnterPassPhraseDialog(string domainID, SimiasWebService simiasws) : base()
 		{
			this.DomainID = domainID;
		        this.simws = simiasws;
			SetupDialog();
		}

        /// <summary>
        /// Setup Dialog - Customize
        /// </summary>
		private void SetupDialog()
		{
			this.Title = Util.GS("iFolder passphrase");
			this.Icon = new Gdk.Pixbuf(Util.ImagesPath("ifolder16.png"));
			this.HasSeparator = false;
//			this.BorderWidth = 10;
			this.SetDefaultSize (450, 100);
	//		this.Resizable = false;
			this.Modal = true;
			this.DestroyWithParent = true;
			this.DefaultResponse = ResponseType.Ok;

			//-----------------------------
			// Add iFolderGraphic
			//-----------------------------
			HBox imagebox = new HBox();
			imagebox.Spacing = 0;
			iFolderBanner = new Image(
					new Gdk.Pixbuf(Util.ImagesPath("ifolder-banner.png")));
			imagebox.PackStart(iFolderBanner, false, false, 0);

			ScaledPixbuf = 
				new Gdk.Pixbuf(Util.ImagesPath("ifolder-banner-scaler.png"));
			iFolderScaledBanner = new Image(ScaledPixbuf);
			iFolderScaledBanner.ExposeEvent += 
					new ExposeEventHandler(OnBannerExposed);
			imagebox.PackStart(iFolderScaledBanner, true, true, 0);
			this.VBox.PackStart (imagebox, false, true, 0);

			Table table = new Table(8, 3, false);
			this.VBox.PackStart(table, false, false, 0);
			table.ColumnSpacing = 6;
			table.RowSpacing = 6;
			table.BorderWidth = 12;

			// Row 1 
			Label l = new Label(Util.GS("Enter the passphrase")+":");
			table.Attach(l,0,3, 0,1,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.LineWrap = true;
			l.Xalign = 0.0F;
            
			// Row 2 
			table.Attach(new Label(""), 0,1, 1,2,
				AttachOptions.Fill, 0,12,0); // spacer
			l = new Label(Util.GS("Domain Name")+":");
			table.Attach(l, 1,2, 1,2,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.LineWrap = true;
			l.Xalign = 0.0F;
            DomainInformation info = simws.GetDomainInformation(DomainID);
			l = new Label(info.Name + " (" + info.Host + ")");
			table.Attach(l, 2,3, 1,2,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.LineWrap = true;
			l.Xalign = 0.0F;

			// Row 3
			table.Attach(new Label(""), 0,1, 2,3,
				AttachOptions.Fill, 0,12,0); // spacer
			l = new Label(Util.GS("_Passphrase:"));
			table.Attach(l,1,2, 2,3,
				AttachOptions.Shrink | AttachOptions.Fill, 0,0,0);
			l.Xalign = 0.0F;
			PassPhraseEntry = new Entry();
			PassPhraseEntry.Visibility = false;
			PassPhraseEntry.Changed += new EventHandler(OnFieldsChanged);
			table.Attach(PassPhraseEntry, 2,3, 2,3,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.MnemonicWidget = PassPhraseEntry;

			// Row 4
			l = new Label(Util.GS("_Re-type passphrase:"));
			table.Attach(l, 1,2, 3,4, 
				AttachOptions.Shrink | AttachOptions.Fill, 0,0,0);
			l.Xalign = 0.0F;
			PassPhraseVerifyEntry = new Entry();
			PassPhraseVerifyEntry.Visibility = false;
			PassPhraseVerifyEntry.Changed += new EventHandler(OnFieldsChanged);
			table.Attach(PassPhraseVerifyEntry, 2,3, 3,4,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.MnemonicWidget = PassPhraseVerifyEntry;
			
			// Row 5
			savePassPhraseButton = new CheckButton(Util.GS("Remember passphrase"));	
			table.Attach(savePassPhraseButton, 2,3, 4,5,
				AttachOptions.Shrink | AttachOptions.Fill, 0,0,0);

			// Row 6
			l = new Label(Util.GS("Select Passphrase Recovery Agent")+":");
			table.Attach(l, 0,3, 5,6,
				AttachOptions.Fill | AttachOptions.Expand, 0,0,0);
			l.LineWrap = true;
			l.Xalign = 0.0F;
			// Row 7
			RATreeView = new iFolderTreeView ();
			ScrolledWindow sw = new ScrolledWindow();
			sw.ShadowType = Gtk.ShadowType.EtchedIn;
			sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.Add(RATreeView);

			DomainController domainController = DomainController.GetDomainController();
			RATreeStore = new ListStore(typeof(string));
			RATreeView.Model = RATreeStore;
                        RAList = domainController.GetRAList(DomainID);
			iFolderWebService ifws = DomainController.GetiFolderService();
			TreeIter listiter = TreeIter.Zero;
			bool selected = false;
			try{
				ifws.ChangePassword(DomainID, null, null);
			 	listiter = RATreeStore.AppendValues(Util.GS("Server_Default"));
				selected = true;
			}
			catch{ }

			if( RAList == null)
			{
				Debug.PrintLine(" no recovery agent present:");
			}
			else
				Debug.PrintLine("Recovery agent present");
                        foreach (string raagent in RAList )
			{
				Debug.PrintLine(String.Format("raagent:{0}", raagent));
                            RATreeStore.AppendValues (raagent);
			}
			// RA Name Column
			TreeViewColumn raNameColumn = new TreeViewColumn();
			raNameColumn.Title = Util.GS("Recovery Agents");
			CellRendererText cr = new CellRendererText();
			cr.Xpad = 5;
			raNameColumn.PackStart(cr, false);
			raNameColumn.SetCellDataFunc(cr,
						     new TreeCellDataFunc(RANameCellTextDataFunc));
			raNameColumn.Resizable = true;
			raNameColumn.MinWidth = 250;

			RATreeView.AppendColumn(raNameColumn);

			RATreeView.Selection.Mode = SelectionMode.Single;

 			table.Attach(sw, 0,3, 6,8,
 				AttachOptions.Expand | AttachOptions.Fill, 0,0,0);


			this.VBox.ShowAll();
	
			if( selected)
				RATreeView.Selection.SelectIter(listiter);	

			this.AddButton(Stock.Cancel, ResponseType.Cancel);
			this.AddButton(Stock.Ok, ResponseType.Ok);
			this.SetResponseSensitive(ResponseType.Ok, false);
			this.DefaultResponse = ResponseType.Ok;
		}
		private void RANameCellTextDataFunc (Gtk.TreeViewColumn tree_column,
				Gtk.CellRenderer cell, Gtk.TreeModel tree_model,
				Gtk.TreeIter iter)
		{
			string value = (string) tree_model.GetValue(iter, 0);
			((CellRendererText) cell).Text = value;
		}

        /// <summary>
        /// Event handler for Banner Exposed event
        /// </summary>
		private void OnBannerExposed(object o, ExposeEventArgs args)
		{
			if(args.Event.Count > 0)
				return;

			Gdk.Pixbuf spb = 
				ScaledPixbuf.ScaleSimple(iFolderScaledBanner.Allocation.Width,
										iFolderScaledBanner.Allocation.Height,
										Gdk.InterpType.Nearest);

			Gdk.GC gc = new Gdk.GC(iFolderScaledBanner.GdkWindow);

			spb.RenderToDrawable(iFolderScaledBanner.GdkWindow,
											gc,
											0, 0,
											args.Event.Area.X,
											args.Event.Area.Y,
											args.Event.Area.Width,
											args.Event.Area.Height,
											Gdk.RgbDither.Normal,
											0, 0);
		}

        /// <summary>
        /// Event handler for Fields Changed event
        /// </summary>
		private void OnFieldsChanged(object obj, EventArgs args)
		{
			bool enableOK = false;

			if( (PassPhraseEntry.Text.Length > 0) &&
				(PassPhraseVerifyEntry.Text.Length > 0 ) )
				enableOK = true;

			this.SetResponseSensitive(ResponseType.Ok, enableOK);
		}
	}
}
