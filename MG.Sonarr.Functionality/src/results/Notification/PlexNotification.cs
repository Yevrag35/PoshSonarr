using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public sealed class PlexNotification : Notification
    {
        [JsonIgnore]
        public override int NotificationId { get; protected private set; }

        public PlexNotification(string host, int port, string username, string password, bool updateLibrary, bool useSsl)
        {
            this.Name = "Plex";
            this.Implementation = "PlexServer";
            this.ImplementationName = "Plex Media Server";
            this.InfoLink = new Uri("https://github.com/Sonarr/Sonarr/wiki/Supported-Notifications#plexserver", UriKind.Absolute);
            this.SupportsOnDownload = true;
            this.SupportsOnGrab = true;
            this.SupportsOnRename = true;
            this.SupportsOnUpgrade = true;
            this.OnGrab = false;
            this.Fields = new FieldCollection(this.MakePlexFields(host, port, username, password, updateLibrary, useSsl));
            this.ConfigContract = "PlexServerSettings";
        }

        private Field[] MakePlexFields(string host, int port, string username, string password, bool updateLibrary, bool useSsl)
        {
            return new Field[6]
            {
                new Field(0, "Host", "Host", host, FieldType.TextBox, false),
                new Field(1, "Port", "Port", port, FieldType.TextBox, false),
                new Field(2, "Username", "Username", username, FieldType.TextBox, false),
                new Field(3, "Password", "Password", password, FieldType.Password, false),
                new Field(4, "UpdateLibrary", "Update Library", updateLibrary, FieldType.CheckBox, false),
                new Field(5, "UseSsl", "Use SSL", useSsl, FieldType.CheckBox, false)
            };
        }
    }
}
