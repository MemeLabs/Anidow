using System.IO;
using Anidow.Enums;
using Anidow.Model;
using FluentValidation;

namespace Anidow.Validators
{
    public class SettingsViewModelValidator : AbstractValidator<SettingsModel>
    {
        public SettingsViewModelValidator()
        {
            RuleFor(settings => settings.AnimeFolder).Must(Directory.Exists);
            RuleFor(settings => settings.RefreshTime).GreaterThanOrEqualTo(1);
            RuleFor(settings => settings.QBitTorrent.Host).NotEmpty()
                                                          .When(settings =>
                                                              settings.TorrentClient == TorrentClient.QBitTorrent);
            RuleFor(settings => settings.QBitTorrent.Port).GreaterThanOrEqualTo(1)
                                                          .When(settings =>
                                                              settings.TorrentClient == TorrentClient.QBitTorrent);
        }
    }
}