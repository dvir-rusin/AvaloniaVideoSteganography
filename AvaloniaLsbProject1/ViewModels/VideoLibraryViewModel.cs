﻿using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using AvaloniaLsbProject1.Services;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Input;
using System;
using Avalonia.Media;    // for IBrush, SolidColorBrush


namespace AvaloniaLsbProject1.ViewModels
{
    public partial class VideoLibraryViewModel : ObservableObject
    {

        // --- New observable properties for feedback UI ---
        [ObservableProperty] public IBrush? errorBorderColor;
        [ObservableProperty] public IBrush? errorColor;
        [ObservableProperty] public IBrush? successOrErrorTextColor;
        [ObservableProperty] public string? successOrError;
        [ObservableProperty] public string? errorMessage;

        private const string MasterPath = "C:\\\\Projects\\\\gitGames\\\\AvaloniaLsbProject1\\\\AvaloniaLsbProject1\\\\Json\\\\MasterKey.txt";
        private const string StoragePath = "C:\\\\Projects\\\\gitGames\\\\AvaloniaLsbProject1\\\\AvaloniaLsbProject1\\\\Json\\\\VideoKeyStorage.json";
        
        [ObservableProperty] private bool isMasterKeySet;
        [ObservableProperty] private bool isUnlocked;
        [ObservableProperty] private string newMasterPassword;
        [ObservableProperty] private string masterPassword;
        [ObservableProperty]
        private ObservableCollection<VideoEntry> videoEntries
            = new();
        public bool IsMasterKeyNotSet => !IsMasterKeySet;

        private Dictionary<string, string> _encryptedStore = new();

        public ICommand SetMasterPasswordCommand { get; }
        public ICommand EnterMasterPasswordCommand { get; }

        public VideoLibraryViewModel()
        {
            // 1) Detect master-key file
            IsMasterKeySet = File.Exists(MasterPath);
            
            // 2) Wire commands
            SetMasterPasswordCommand = new RelayCommand(
            _ => SetMasterKey(),
            _ => !IsMasterKeySet
            );
            EnterMasterPasswordCommand = new RelayCommand(
                _ => UnlockWithKey(),
                _ => IsMasterKeySet
            );


            // 3) Preload encrypted JSON (no UI update yet)
            LoadEncryptedFromJson();
        }

        partial void OnIsMasterKeySetChanged(bool value)
        {
            OnPropertyChanged(nameof(IsMasterKeyNotSet));
            // re-enable/disable the two password buttons:
            (SetMasterPasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EnterMasterPasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private static string ComputeHash(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plain));
            return Convert.ToBase64String(bytes);
        }

        private void SetMasterKey()
        {
            // ensure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(MasterPath)!);

            // Hash the new password before saving:
            var hashed = ComputeHash(NewMasterPassword);
            File.WriteAllText(MasterPath, hashed);

            // Show success
            DisplaySuccessMessage("Master password created!");

            IsMasterKeySet = true;
            NewMasterPassword = string.Empty;
        }

        private void UnlockWithKey()
        {
            try
            {
                // Read the *hash* and compare with hash of entered password
                var storedHash = File.ReadAllText(MasterPath);
                if (storedHash != ComputeHash(MasterPassword))
                    throw new CryptographicException("Wrong password");

                // Use the stored hash as the encryption/decryption key
                //bool worked = EncryptionAes.EncryptExistingVideoKeyStorage(storedHash);

                // Decrypt every entry
                var list = new ObservableCollection<VideoEntry>();
                foreach (var kv in _encryptedStore)
                {
                    var name = EncryptionAes.Decrypt(kv.Key, storedHash);
                    var pwd = EncryptionAes.Decrypt(kv.Value, storedHash);
                    list.Add(new VideoEntry { VideoName = name, Password = pwd });
                }
                IsUnlocked = true;
                VideoEntries = list;
                // Show success
                DisplaySuccessMessage("Library unlocked!");
            }
            catch
            {
                // Bad password: stay locked
                VideoEntries.Clear();
                IsUnlocked = false;
                // Show error
                DisplayErrorMessage("Wrong master password");
            }
        }

        private void LoadEncryptedFromJson()
        {
            if (!File.Exists(StoragePath))
                return;

            var json = File.ReadAllText(StoragePath);
            _encryptedStore = JsonConvert
                .DeserializeObject<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }

        private void DisplayErrorMessage(string message)
        {
            ErrorBorderColor = new SolidColorBrush(Color.Parse("#FF4444"));
            ErrorColor = new SolidColorBrush(Color.Parse("#2a1e1e"));
            SuccessOrErrorTextColor = new SolidColorBrush(Color.Parse("#FF4444"));
            SuccessOrError = "Error";
            ErrorMessage = message;
        }

        private void DisplaySuccessMessage(string message)
        {
            ErrorBorderColor = new SolidColorBrush(Color.Parse("#44FF44"));
            ErrorColor = new SolidColorBrush(Color.Parse("#1e2a1e"));
            SuccessOrErrorTextColor = new SolidColorBrush(Color.Parse("#44FF44"));
            SuccessOrError = "Success";
            ErrorMessage = message;
        }
    }

}
