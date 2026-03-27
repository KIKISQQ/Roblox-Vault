using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace RobloxVault.Models
{
    public class RobloxAccount : INotifyPropertyChanged
    {
        private string _displayName = "";
        private string _username = "";
        private string _encryptedCookie = "";
        private string? _avatarUrl;
        private string _presenceStatus = "Offline";
        private long _userId;
        private bool _isLoadingPresence;
        private bool _isPinned;
        private string _lastPlaceId = "3016661674";
        private long _robuxBalance = -1;
        private string _description = "";
        private string _rlDescription = "";
        private List<string> _rlTags = new();
        private bool _isSelected;

        private ObservableCollection<CustomCard> _customCards = new();


        // dont need to save this, its only for ui selection
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string EncryptedCookie
        {
            get => _encryptedCookie;
            set { _encryptedCookie = value; OnPropertyChanged(); }
        }

        public string? AvatarUrl
        {
            get => _avatarUrl;
            set { _avatarUrl = value; OnPropertyChanged(); }
        }

        public string PresenceStatus
        {
            get => _presenceStatus;
            set { _presenceStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(PresenceColor)); }
        }

        public long UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public bool IsLoadingPresence
        {
            get => _isLoadingPresence;
            set { _isLoadingPresence = value; OnPropertyChanged(); }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; OnPropertyChanged(); OnPropertyChanged(nameof(PinIcon)); }
        }

        public string LastPlaceId
        {
            get => _lastPlaceId;
            set { _lastPlaceId = value; OnPropertyChanged(); }
        }

        public long RobuxBalance
        {
            get => _robuxBalance;
            set { _robuxBalance = value; OnPropertyChanged(); OnPropertyChanged(nameof(RobuxDisplay)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDescription)); }
        }

        public bool HasCustomCards => _customCards.Count > 0;
        public ObservableCollection<CustomCard> CustomCards
        {
            get => _customCards;
            set { _customCards = value ?? new ObservableCollection<CustomCard>(); OnPropertyChanged(); OnPropertyChanged(nameof(HasCustomCards)); }
        }

        public string RLDescription
        {
            get => _rlDescription;
            set { _rlDescription = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasRLDescription)); }
        }

        public List<string> RLTags
        {
            get => _rlTags;
            set { _rlTags = value ?? new List<string>(); NotifyAllRLProps(); }
        }

        private static readonly string[] ArtifactTags = { "MA", "PD", "WKA", "Lannis" };

        public int ActiveArtifactCount => _rlTags.Count(t => ArtifactTags.Contains(t));

        public bool ToggleRLTag(string tag)
        {
            bool isArtifact = ArtifactTags.Contains(tag);

            if (_rlTags.Contains(tag))
            {
                _rlTags.Remove(tag);
            }
            else
            {
                if (isArtifact && ActiveArtifactCount >= 2)
                    return false;
                _rlTags.Add(tag);
            }

            NotifyAllRLProps();
            return true;
        }

        private void NotifyAllRLProps()
        {
            OnPropertyChanged(nameof(RLTags));
            OnPropertyChanged(nameof(ActiveArtifactCount));
            OnPropertyChanged(nameof(HasRLTags));
            OnPropertyChanged(nameof(IsMA_Active));
            OnPropertyChanged(nameof(IsPD_Active));
            OnPropertyChanged(nameof(IsWKA_Active));
            OnPropertyChanged(nameof(IsLannis_Active));
            OnPropertyChanged(nameof(IsSilver_Active));
            OnPropertyChanged(nameof(IsMA_Visible));
            OnPropertyChanged(nameof(IsPD_Visible));
            OnPropertyChanged(nameof(IsWKA_Visible));
            OnPropertyChanged(nameof(IsLannis_Visible));
        }


        public void NotifyCardsChanged()
        {
            OnPropertyChanged(nameof(CustomCards));
            OnPropertyChanged(nameof(HasCustomCards));
        }

        // Active state per tag
        public bool IsMA_Active     => _rlTags.Contains("MA");
        public bool IsPD_Active     => _rlTags.Contains("PD");
        public bool IsWKA_Active    => _rlTags.Contains("WKA");
        public bool IsLannis_Active => _rlTags.Contains("Lannis");
        public bool IsSilver_Active => _rlTags.Contains("Silver");

        // artifact cards: hide when cap (2) is reached 
        public bool IsMA_Visible     => IsMA_Active     || ActiveArtifactCount < 2;
        public bool IsPD_Visible     => IsPD_Active     || ActiveArtifactCount < 2;
        public bool IsWKA_Visible    => IsWKA_Active    || ActiveArtifactCount < 2;
        public bool IsLannis_Visible => IsLannis_Active || ActiveArtifactCount < 2;
        // silver always visible
        public bool IsSilver_Visible => true;

        public bool HasRLTags        => _rlTags.Count > 0;
        public bool HasRLDescription => !string.IsNullOrWhiteSpace(_rlDescription);


        public string PinIcon        => IsPinned ? "📌" : "📍";
        public string RobuxDisplay   => RobuxBalance >= 0 ? $"R$ {RobuxBalance:N0}" : "R$ —";
        public bool   HasDescription => !string.IsNullOrWhiteSpace(_description);

        public string PresenceColor => PresenceStatus switch
        {
            "Online"    => "#3DBA6F",
            "In Game"   => "#FF6B35",
            "In Studio" => "#4A9EFF",
            _           => "#4A4A5A"
        };

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}