﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Input;
using Anidow.Database;
using Anidow.Enums;
using Microsoft.EntityFrameworkCore;
using Stylet;

namespace Anidow.Pages.Components.Notify
{
    public class NotifyAddViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        public NotifyItem Item { get; set; } = new();
        public int ItemId { get; set; }
        public BindableCollection<NotifyItemKeyword> Keywords { get; set; }
        public bool IsEdit { get; set; }

        public NotifyAddViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            Title = "Add";
            Keywords = new BindableCollection<NotifyItemKeyword>(Item.Keywords);
        }

        public NotifyAddViewModel(NotifyItem item, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            Item = item;
            Title = "Edit";
            IsEdit = true;
            Keywords = new BindableCollection<NotifyItemKeyword>(Item.Keywords);
        }

        public string Title { get; set; }

        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public bool MustMatch { get; set; }

        public string Keyword { get; set; }
        public string ErrorMessage { get; set; }

        private bool CanCanAddMethod()
        {
            if (string.IsNullOrEmpty(Item.Name))
            {
                ErrorMessage = "Name can not be empty";
                return false;
            }

            if (Item.Keywords.Count < 1)
            {
                ErrorMessage = "You need at least one keyword";
                return false;
            }

            ErrorMessage = null;
            return true;
        }

        public bool CanAdd => !IsEdit && CanCanAddMethod();
        public async Task Add()
        {
            await using var db = new TrackContext();
            await db.NotifyItems.AddAsync(Item);
            await db.SaveChangesAsync();

            _eventAggregator.PublishOnUIThread(new NotifyItemAddOrUpdateEvent
            {
                Item = Item,
            });

            RequestClose(true);
        }

        public async Task AddKeyword()
        {
            if (string.IsNullOrEmpty(Keyword))
            {
                return;
            }

            var newKeyword = new NotifyItemKeyword
            {
                NotifyItemId = Item.Id,
                Word = Keyword.Trim(),
                IsRegex = UseRegex,
                IsCaseSensitive = CaseSensitive,
                MustMatch = MustMatch,
            };
            
            if (IsEdit)
            {
                await using var db = new TrackContext();
                db.NotifyItemKeywords.Add(newKeyword);
                await db.SaveChangesAsync();
            }
            
            Item.Keywords.Add(newKeyword);
            Keywords.Add(newKeyword);

            Keyword = string.Empty;
            NotifyOfPropertyChange(() => CanAdd);
            NotifyOfPropertyChange(() => Item.Keywords);
        }

        public async Task RemoveKeyword(NotifyItemKeyword keyword)
        {
            Item.Keywords.Remove(keyword);
            Keywords.Remove(keyword);

            if (IsEdit)
            {
                await using var db = new TrackContext();
                db.Attach(keyword);
                db.Remove(keyword);
                await db.SaveChangesAsync();
            }

            NotifyOfPropertyChange(() => CanAdd);
            NotifyOfPropertyChange(() => Item.Keywords);
        }
        
        public async Task Close()
        {
            if (IsEdit)
            {
                await using var db = new TrackContext();
                db.Attach(Item);
                db.Update(Item);
                await db.SaveChangesAsync();
            }
            RequestClose(true);
        }


        public void Keyword_OnPreviewKeyDown(object _, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }
            AddKeyword().Wait();
            e.Handled = true;
        }
    }
}