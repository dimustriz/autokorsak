using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using WPF.JoshSmith.ServiceProviders.UI;

using System.Globalization;

using Tourtoss.BE;

namespace AutoKorsak
{
    public partial class TournamentPropertiesWindow : Window
    {
        private Tournament _tournament;
        private bool _isCreated;
        private WindowHelper.ResultHandler OnResult;
        private ListViewDragDropManager<WallListMemberDescriptior> dragMgr;
        private ListViewDragDropManager<SortCriterionDescriptior> dragMgr2;

        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
            pnlButtons.Background = body;
            pnlButtons.BorderBrush = border;
            pnlHandicapButtons.Background = body;
            pnlHandicapButtons.BorderBrush = border;
            pnlHandicap.BorderBrush = border;
            pnlMainProps.BorderBrush = border;
            pnlPlacementCriteria.BorderBrush = border;
            pnlRestrictions.BorderBrush = border;
            pnlCalculation.BorderBrush = border;
            pnlCalculationButtons.Background = body;
            pnlCalculationButtons.BorderBrush = border;
        }

        public void SetContext(Tournament tournament)
        {
            _tournament = tournament;
            _isCreated = _tournament.IsCreated;
            _tournament.IsCreated = true;
            DataContext = _tournament;

            _tournament.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
        }

        public TournamentPropertiesWindow(Tournament tournament, WindowHelper.ResultHandler onResult)
        {
            InitializeComponent();
            InitPalette();

            OnResult = onResult;
            SetContext(tournament);
            InitLangs();
        }

        private void OnTournamentSystemUpdate()
        {
            var visible = System.Windows.Visibility.Visible;
            var hidden = System.Windows.Visibility.Collapsed;

            if (_tournament.TournamentSystemRound)
            {
                lblNumberOfPlayers.Visibility = visible;
                txtNumberOfPlayers.Visibility = visible;
                lblNumberOfTeamPlayers.Visibility = hidden;
                txtNumberOfTeamPlayers.Visibility = hidden;
                lblNumberOfRounds.Visibility = hidden;
                txtNumberOfRounds.Visibility = hidden;
                tabRestrictions.Visibility = hidden;
            }
            else
            if (_tournament.TournamentSystemScheveningen)
            {
                lblNumberOfPlayers.Visibility = hidden;
                txtNumberOfPlayers.Visibility = hidden;
                lblNumberOfTeamPlayers.Visibility = visible;
                txtNumberOfTeamPlayers.Visibility = visible;
                lblNumberOfRounds.Visibility = hidden;
                txtNumberOfRounds.Visibility = hidden;
                tabRestrictions.Visibility = hidden;
            }
            else
            {
                lblNumberOfPlayers.Visibility = hidden;
                txtNumberOfPlayers.Visibility = hidden;
                lblNumberOfTeamPlayers.Visibility = hidden;
                txtNumberOfTeamPlayers.Visibility = hidden;
                lblNumberOfRounds.Visibility = visible;
                txtNumberOfRounds.Visibility = visible;
                tabRestrictions.Visibility = visible;
            }

            if (_tournament.TournamentSystemMcMahon)
                tabGroups.Visibility = visible;
            else
                tabGroups.Visibility = hidden;

        }

        private void OnHandicapUpdate()
        {
            var visible = System.Windows.Visibility.Visible;
            var hidden = System.Windows.Visibility.Collapsed;

            if (_tournament.HandicapUsed)
                tabHandicap.Visibility = visible;
            else
                tabHandicap.Visibility = hidden;
        }

        private void OnCustomizeCalculationUpdate()
        {
            var visible = System.Windows.Visibility.Visible;
            var hidden = System.Windows.Visibility.Collapsed;

            if (_tournament.CustomizeCalculation)
                tabCalculation.Visibility = visible;
            else
                tabCalculation.Visibility = hidden;
        }
        
        private void InitLangs()
        {
            if (string.IsNullOrEmpty(_tournament.Name))
                this.Title = TournamentView.AppName + " - " + LangResources.LR.TournamentProperties;
            else
                this.Title = TournamentView.AppName + " - " + LangResources.LR.TournamentProperties + " - " + _tournament.Name;

            InitializeCriterias();
            InitializeWallListMembers();

            OnTournamentSystemUpdate();
            OnHandicapUpdate();
            OnCustomizeCalculationUpdate();
        }

        private void InitializeWallListMembers()
        {
            var members = new ObservableCollection<WallListMemberDescriptior>();

            List<Entity> entities = new List<Entity>();
            foreach (var item in _tournament.Walllist.Columns)
                entities.Add(item.Id);
            foreach (var item in _tournament.EntitiesMin)
                if (!entities.Contains(item))
                    entities.Add(item);
            foreach (var item in _tournament.EntitiesCriteria)
                if (!entities.Contains(item))
                    entities.Add(item);
            foreach (Entity item in Enum.GetValues(typeof(Entity)))
                if (!entities.Contains(item) && !_tournament.EntitiesOutOfWallist.Contains(item))
                    entities.Add(item);

            foreach (Entity it in entities)
            {
                if (members.FirstOrDefault(val => val != null && val.Id == it) == null &&
                    !_tournament.EntitiesCriteria.Contains(it))
                {
                    var member = new WallListMemberDescriptior() { Id = it };
                    member.Enabled = true;
                    if (_tournament.Walllist.Columns.Find(item => item.Id == member.Id) != null)
                        member.Active = true;
                    if (_tournament.EntitiesMin.Contains(member.Id))
                    {
                        member.Active = true;
                        member.Enabled = false;
                    }
                    if (_tournament.EntitiesHidden.Contains(it))
                        continue;

                    if (_tournament.EntitiesCriteria.Contains(it))
                        continue;

                    members.Add(member);
                }
            }

            this.lvWallList.ItemsSource = members;

            // This is all that you need to do, in order to use the ListViewDragManager.
            this.dragMgr = new ListViewDragDropManager<WallListMemberDescriptior>(this.lvWallList);

            // Turn the ListViewDragManager on and off. 
            this.dragMgr.ListView = this.lvWallList;

            // Show and hide the drag adorner.
            this.dragMgr.ShowDragAdorner = true;

            // Hook up events on both ListViews to that we can drag-drop
            // items between them.
            this.lvWallList.DragEnter += OnListViewDragEnter;
            this.lvWallList.Drop += OnListViewDrop;
        }

        private void InitializeCriterias()
        {
            var members = new ObservableCollection<SortCriterionDescriptior>();

            List<Entity> entities = new List<Entity>();
            
            foreach (var item in _tournament.Walllist.SortCriterion)
                entities.Add(item.Id);

            foreach (var item in _tournament.EntitiesCriteria)
                if (!entities.Contains(item))
                    entities.Add(item);

            foreach (Entity it in entities)
            {
                if (members.FirstOrDefault(val => val != null && val.Id == it) == null)
                {
                    var member = new SortCriterionDescriptior() { Id = it };
                    member.Enabled = true;
                    if (_tournament.Walllist.SortCriterion.Find(item => item.Id == member.Id) != null)
                        member.Active = true;
                    if (_tournament.EntitiesHidden.Contains(it))
                        continue;

                    members.Add(member);
                }
            }

            this.lvSortCriterion.ItemsSource = members;

            // This is all that you need to do, in order to use the ListViewDragManager.
            this.dragMgr2 = new ListViewDragDropManager<SortCriterionDescriptior>(this.lvSortCriterion);

            // Turn the ListViewDragManager on and off. 
            this.dragMgr2.ListView = this.lvSortCriterion;

            // Show and hide the drag adorner.
            this.dragMgr2.ShowDragAdorner = true;

            // Hook up events on both ListViews to that we can drag-drop
            // items between them.
            this.lvSortCriterion.DragEnter += OnListViewDragEnter;
            this.lvSortCriterion.Drop += OnListViewDrop;
        }

        #region dragMgr_ProcessDrop

		// Performs custom drop logic for the top ListView.
        void dragMgr_ProcessDrop(object sender, ProcessDropEventArgs<WallListMemberDescriptior> e)
		{
			// This shows how to customize the behavior of a drop.
			// Here we perform a swap, instead of just moving the dropped item.

			int higherIdx = Math.Max( e.OldIndex, e.NewIndex );
			int lowerIdx = Math.Min( e.OldIndex, e.NewIndex );

			if( lowerIdx < 0 )
			{
				// The item came from the lower ListView
				// so just insert it.
				e.ItemsSource.Insert( higherIdx, e.DataItem );
			}
			else
			{
				// null values will cause an error when calling Move.
				// It looks like a bug in ObservableCollection to me.
				if( e.ItemsSource[lowerIdx] == null ||
					e.ItemsSource[higherIdx] == null )
					return;

				// The item came from the ListView into which
				// it was dropped, so swap it with the item
				// at the target index.
				e.ItemsSource.Move( lowerIdx, higherIdx );
				e.ItemsSource.Move( higherIdx - 1, lowerIdx );
			}

			// Set this to 'Move' so that the OnListViewDrop knows to 
			// remove the item from the other ListView.
			e.Effects = DragDropEffects.Move;
		}

		#endregion // dragMgr_ProcessDrop

		#region OnListViewDragEnter

		// Handles the DragEnter event for both ListViews.
		void OnListViewDragEnter( object sender, DragEventArgs e )
		{
			e.Effects = DragDropEffects.Move;
		}

		#endregion // OnListViewDragEnter

		#region OnListViewDrop

		// Handles the Drop event for both ListViews.
		void OnListViewDrop( object sender, DragEventArgs e )
		{
			if( e.Effects == DragDropEffects.None )
				return;
            /*
			Task task = e.Data.GetData( typeof( Task ) ) as Task;
			if( sender == this.listView )
			{
				if( this.dragMgr.IsDragInProgress )
					return;

				// An item was dragged from the bottom ListView into the top ListView
				// so remove that item from the bottom ListView.
				(this.listView2.ItemsSource as ObservableCollection<Task>).Remove( task );
			}
			else
			{
				if( this.dragMgr2.IsDragInProgress )
					return;

				// An item was dragged from the top ListView into the bottom ListView
				// so remove that item from the top ListView.
				(this.listView.ItemsSource as ObservableCollection<Task>).Remove( task );
			}
            */
		}

		#endregion // OnListViewDrop


        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            _tournament.LowerMacMahonBarLevelExt = _tournament.LowerMacMahonBarLevelExt;
            _tournament.UpperMacMahonBarLevelExt = _tournament.UpperMacMahonBarLevelExt;
            
            InitLangs();
        }

        private void Apply()
        {
            _tournament.IsCreated = _isCreated;

            _tournament.Walllist.SortCriterion.Clear();
            foreach (var item in lvSortCriterion.Items)
            {
                var descr = item as SortCriterionDescriptior;
                if (descr.Active)
                {
                    var soDscr = new SortCriterionDescriptior() { Id = descr.Id };
                    _tournament.Walllist.SortCriterion.Add(soDscr);
                }
            }

            _tournament.Walllist.Columns.Clear();
            foreach (var item in lvWallList.Items)
            {
                var descr = item as WallListMemberDescriptior;
                if (descr.Active)
                {
                    var member = new WallListMemberDescriptior() { Id = descr.Id };
                    _tournament.Walllist.Columns.Add(member);
                }
            }

            if (OnResult != null)
                OnResult(ReturnResult.Ok, _tournament);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(ReturnResult.Cancel, null); 
            Close();
        }

        private void form_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings();
        }

        private void form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.SaveSettings();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            OnTournamentSystemUpdate();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            OnHandicapUpdate();
            OnCustomizeCalculationUpdate();
        }

        bool OnAssistantWindowReturn(ReturnResult ret, object value)
        {
            var scheme = value as TournamentScheme;
            switch (ret)
            {
                case ReturnResult.Apply:
                case ReturnResult.Ok:
                    {
                        _tournament.NumberOfRounds = scheme.RoundsAmount;

                        switch (scheme.PrefferableSystem)
                        {
                            case TournamentKind.McMahon:
                                {
                                    _tournament.TournamentSystemMcMahon = true;
                                    _tournament.UpperMacMahonBarAmount = scheme.TopGroupParticipantsAmount;
                                    _tournament.UpperMacMahonBar = scheme.TopGroupParticipantsAmount > 0;
                                    _tournament.UpperMacMahonBarByAmount = scheme.TopGroupParticipantsAmount > 0;
                                    break;
                                }
                            case TournamentKind.Round:
                                {
                                    _tournament.TournamentSystemRound = true;
                                    break;
                                }
                            case TournamentKind.Swiss:
                                {
                                    _tournament.TournamentSystemSwiss = true;
                                    break;
                                }
                            case TournamentKind.Scheveningen:
                                {
                                    _tournament.TournamentSystemScheveningen = true;
                                    break;
                                }
                        }

                        OnTournamentSystemUpdate();

                        _tournament.OnPropertyChanged("NumberOfRounds");
                        _tournament.OnPropertyChanged("NumberOfPlayers");
                        break;
                    }
            }

            return true;
        }

        private void ExecuteAssistantWindow(Tournament tournament, TournamentScheme scheme)
        {
            AssistantWindow dlg = App.GetOpenedWindow(typeof(AssistantWindow)) as AssistantWindow;
            if (dlg == null)
                dlg = new AssistantWindow(tournament, scheme, OnAssistantWindowReturn);
            else
                dlg.SetContext(tournament, scheme);
            dlg.ShowWindow();
        }

        private void btnAssistant_Click(object sender, RoutedEventArgs e)
        {
            var scheme = new TournamentScheme()
            {
                ParticipantsAmount = _tournament.Players.Count,
                RoundsAmount = _tournament.Tours.Count
            };

            ExecuteAssistantWindow(_tournament, scheme);
        }

    }
}
