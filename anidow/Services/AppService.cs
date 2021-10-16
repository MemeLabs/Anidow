using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anidow.Annotations;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Anidow.Services
{
    public class AppService
    {
        private readonly ILogger _logger;

        public AppService(ILogger logger)
        {
            _logger = logger;
        }

        public AppState State { get; set; }

        public async Task Initialize()
        {
            await using var db = new TrackContext();
            State = await db.AppStates.FirstOrDefaultAsync();
            if (State is null)
            {
                State = new AppState();
                await db.AppStates.AddAsync(State);
                await db.SaveChangesAsync();
            }
            
            State.PropertyChanged += StateOnPropertyChanged;
        }

        private void StateOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debouncer.DebounceAction("AppState:Save", async _ =>
            {
                await using var db = new TrackContext();
                db.Attach(State);
                db.Update(State);
                await db.SaveChangesAsync();
                _logger.Information("Saved AppState to Database");
            }, TimeSpan.FromMilliseconds(100));
        }
    }
}
