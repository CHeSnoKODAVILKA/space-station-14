using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using static Content.Client.Administration.UI.Tabs.PlayerTab.PlayerTabHeader;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Administration.UI.Tabs.PlayerTab
{
    [GenerateTypedNameReferences]
    public sealed partial class PlayerTab : Control
    {
        private const string ArrowUp = "↑";
        private const string ArrowDown = "↓";
        private readonly Color _altColor = Color.FromHex("#292B38");
        private readonly Color _defaultColor = Color.FromHex("#2F2F3B");
        private readonly AdminSystem _adminSystem;
        private readonly List<PlayerTabEntry> _players = new();

        private Header _headerClicked = Header.Username;
        private bool _ascending = true;

        public event Action<ButtonEventArgs>? OnEntryPressed;

        public PlayerTab()
        {
            _adminSystem = EntitySystem.Get<AdminSystem>();
            RobustXamlLoader.Load(this);
            RefreshPlayerList(_adminSystem.PlayerList);

            _adminSystem.PlayerListChanged += RefreshPlayerList;
            _adminSystem.OverlayEnabled += OverlayEnabled;
            _adminSystem.OverlayDisabled += OverlayDisabled;

            OverlayButton.OnPressed += OverlayButtonPressed;

            ListHeader.BackgroundColorPanel.PanelOverride = new StyleBoxFlat(_altColor);
            ListHeader.OnHeaderClicked += HeaderClicked;
        }

        private void OverlayEnabled()
        {
            OverlayButton.Pressed = true;
        }

        private void OverlayDisabled()
        {
            OverlayButton.Pressed = false;
        }

        private void OverlayButtonPressed(ButtonEventArgs args)
        {
            if (args.Button.Pressed)
            {
                _adminSystem.AdminOverlayOn();
            }
            else
            {
                _adminSystem.AdminOverlayOff();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _adminSystem.PlayerListChanged -= RefreshPlayerList;
                _adminSystem.OverlayEnabled -= OverlayEnabled;
                _adminSystem.OverlayDisabled -= OverlayDisabled;

                OverlayButton.OnPressed -= OverlayButtonPressed;

                ListHeader.OnHeaderClicked -= HeaderClicked;
            }
        }

        private void RefreshPlayerList(IReadOnlyList<PlayerInfo> players)
        {
            foreach (var control in _players)
            {
                PlayerList.RemoveChild(control);
            }

            _players.Clear();

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            PlayerCount.Text = $"Players: {playerManager.PlayerCount}";

            var sortedPlayers = new List<PlayerInfo>(players);
            sortedPlayers.Sort(Compare);

            UpdateHeaderSymbols();

            var useAltColor = false;
            foreach (var player in sortedPlayers)
            {
                var entry = new PlayerTabEntry(player.Username,
                    player.CharacterName,
                    player.StartingJob,
                    player.Antag ? "YES" : "NO",
                    new StyleBoxFlat(useAltColor ? _altColor : _defaultColor),
                    player.Connected);
                entry.PlayerUid = player.EntityUid;
                entry.OnPressed += args => OnEntryPressed?.Invoke(args);
                PlayerList.AddChild(entry);
                _players.Add(entry);

                useAltColor ^= true;
            }
        }

        private void UpdateHeaderSymbols()
        {
            ListHeader.ResetHeaderText();
            ListHeader.GetHeader(_headerClicked).Text += $" {(_ascending ? ArrowUp : ArrowDown)}";
        }

        private int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (!_ascending)
            {
                (x, y) = (y, x);
            }

            return _headerClicked switch
            {
                Header.Username => Compare(x.Username, y.Username),
                Header.Character => Compare(x.CharacterName, y.CharacterName),
                Header.Job => Compare(x.StartingJob, y.StartingJob),
                Header.Antagonist => x.Antag.CompareTo(y.Antag),
                _ => 1
            };
        }

        private int Compare(string x, string y)
        {
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private void HeaderClicked(Header header)
        {
            if (_headerClicked == header)
            {
                _ascending = !_ascending;
            }
            else
            {
                _headerClicked = header;
                _ascending = true;
            }

            RefreshPlayerList(_adminSystem.PlayerList);
        }
    }
}