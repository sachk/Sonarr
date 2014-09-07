﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NLog;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;

        protected UsenetClientBase(IHttpClient httpClient,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    IParsingService parsingService,
                                    Logger logger)
            : base(configService, diskProvider, parsingService, logger)
        {
            _httpClient = httpClient;
        }
        
        public override DownloadProtocol Protocol
        {
            get
            {
                return DownloadProtocol.Usenet;
            }
        }

        protected abstract String AddFromNzbFile(RemoteEpisode remoteEpisode, String filename, Byte[] fileContent);

        public override String Download(RemoteEpisode remoteEpisode)
        {
            var url = remoteEpisode.Release.DownloadUrl;
            var filename =  FileNameBuilder.CleanFileName(remoteEpisode.Release.Title) + ".nzb";

            Byte[] nzbData;

            try
            {
                using (var nzb = _httpClient.Get(new HttpRequest(url)).GetStream())
                {
                    nzbData = nzb.ToBytes();
                }
            }
            catch (WebException ex)
            {
                _logger.ErrorException(String.Format("Downloading nzb for episode '{0}' failed ({1})",
                    remoteEpisode.Release.Title, url), ex);

                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading nzb failed", ex);
            }

            _logger.Info("Adding report [{0}] to the queue.", remoteEpisode.Release.Title);
            return AddFromNzbFile(remoteEpisode, filename, nzbData);
        }
    }
}
