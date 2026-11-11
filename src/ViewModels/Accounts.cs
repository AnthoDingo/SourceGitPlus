using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class Accounts : ObservableObject
    {
        public static Accounts Instance { get; } = new Accounts();

        public AvaloniaList<Account> Items
        {
            get;
        } = [];

        public Account Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        private Accounts()
        {
            Load();
        }

        public void Add(Account account)
        {
            Watch(account);
            Items.Add(account);
            Selected = account;
            Save();
            _ = account.RefreshAsync();
        }

        public void RemoveSelected()
        {
            if (_selected == null)
                return;

            Items.Remove(_selected);
            Save();
        }

        public void RefreshAll()
        {
            foreach (Account account in Items)
                _ = account.RefreshAsync();
        }

        private void Watch(Account account)
        {
            account.PropertyChanged += OnAccountPropertyChanged;
        }

        private void OnAccountPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Account.EnableNotifications) || e.PropertyName == nameof(Account.Token))
                Save();
        }

        private static string FilePath
        {
            get => Path.Combine(Native.OS.BasicDirectories.ConfigDir, "accounts.json");
        }

        private void Load()
        {
            if (!File.Exists(FilePath))
                return;

            try
            {
                using FileStream stream = File.OpenRead(FilePath);
                List<Models.AccountRecord> records = JsonSerializer.Deserialize(stream, JsonCodeGen.Default.ListAccountRecord);
                foreach (Models.AccountRecord record in records ?? [])
                {
                    string token = Models.TokenProtector.Unprotect(record.Token);
                    if (token == null)
                        continue;

                    Account account = new Account(record.Provider, GetIcon(record.Provider), record.Username, record.Url, record.AuthType, token, record.EnableNotifications);
                    Watch(account);
                    Items.Add(account);
                }
            }
            catch (Exception)
            {
                // Corrupted file: start with no accounts.
            }
        }

        private void Save()
        {
            List<Models.AccountRecord> records = [];
            foreach (Account account in Items)
            {
                records.Add(new Models.AccountRecord
                {
                    Provider = account.Provider,
                    Url = account.Url,
                    Username = account.Username,
                    AuthType = account.AuthType,
                    Token = Models.TokenProtector.Protect(account.Token),
                    EnableNotifications = account.EnableNotifications,
                });
            }

            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(records, JsonCodeGen.Default.ListAccountRecord));
            }
            catch (Exception)
            {
                // Best effort: accounts stay usable for this session.
            }
        }

        public static Geometry GetIcon(string provider)
        {
            string key = provider.StartsWith("GitHub", StringComparison.Ordinal) ? "Icons.GitHub" :
                provider.StartsWith("GitLab", StringComparison.Ordinal) ? "Icons.Provider.GitLab" :
                provider.StartsWith("Bitbucket", StringComparison.Ordinal) ? "Icons.Provider.Bitbucket" :
                "Icons.Provider.Gitea";

            object icon = null;
            Application.Current?.TryGetResource(key, null, out icon);
            return icon as Geometry;
        }

        private Account _selected;
    }
}
