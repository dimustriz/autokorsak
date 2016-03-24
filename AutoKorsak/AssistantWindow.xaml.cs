using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using Tourtoss.BE;

namespace AutoKorsak
{
    public partial class AssistantWindow : Window
    {
        private Tournament _tournament;
        private TournamentScheme _scheme;
        private bool _isCreated;
        private WindowHelper.ResultHandler OnResult;

        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
            pnlButtons.Background = body;
            pnlButtons.BorderBrush = border;
            pnlInnerBody.BorderBrush = border;
            pnlInnerBody.Background = body;

        }

        public void SetContext(Tournament tournament, TournamentScheme scheme)
        {
            _tournament = tournament;
            _isCreated = _tournament.IsCreated;
            _scheme = scheme;
            _tournament.IsCreated = true;
            DataContext = _scheme;

            _tournament.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
        }

        public AssistantWindow(Tournament tournament, TournamentScheme shceme, WindowHelper.ResultHandler onResult)
        {
            InitializeComponent();
            InitPalette();

            OnResult = onResult;
            SetContext(tournament, shceme);
            InitLangs();
        }

        private void OnTournamentSystemUpdate()
        {

        }

        private void OnHandicapUpdate()
        {
        }

        private void OnCustomizeCalculationUpdate()
        {
        }
        
        private void InitLangs()
        {
            this.Title = TournamentView.AppName + " - " + LangResources.LR.Assistant;

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
                if (!entities.Contains(item) && item != Entity.Num)
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

        }

        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            InitLangs();
            _scheme.OnPropertyChanged("RecommendedScheme");
        }

        private void Apply()
        {
            if (OnResult != null)
                OnResult(ReturnResult.Ok, _scheme);
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

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            OnCustomizeCalculationUpdate();
        }

        private void txtNumberOfPlayers_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int value = 0;
            int.TryParse(txtNumberOfPlayers.Text, out value);
            _scheme.ParticipantsAmount = value;
        }

        private void txtNumberOfPrizes_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int value = 0;
            int.TryParse(txtNumberOfPrizes.Text, out value);
            _scheme.PrizesAmount = value;
        }

        private void txtNumberOfRounds_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int value = 0;
            int.TryParse(txtNumberOfRounds.Text, out value);
            _scheme.RoundsAmount = value;
        }

    }
}
