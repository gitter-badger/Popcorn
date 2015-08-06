﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Model.Movie;
using Popcorn.Model.Subtitle;
using Popcorn.Service.Api;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Popcorn.ViewModel.Subtitles
{
    public class SubtitlesViewModel : ViewModelBase
    {
        #region Properties

        #region Property -> ApiService

        /// <summary>
        /// The service used to consume APIs
        /// </summary>
        private IApiService ApiService { get; }

        #endregion

        #region Property -> Subtitles

        /// <summary>
        /// The available movie's subtitles
        /// </summary>
        private ObservableCollection<Subtitle> _subtitles = new ObservableCollection<Subtitle>();

        public ObservableCollection<Subtitle> Subtitles
        {
            get { return _subtitles; }
            set { Set(() => Subtitles, ref _subtitles, value); }
        }

        #endregion

        #region Property -> SelectedSubtitle

        /// <summary>
        /// The selected movie's subtitle
        /// </summary>
        private Subtitle _selectedSubtitle;

        public Subtitle SelectedSubtitle
        {
            get { return _selectedSubtitle; }
            set
            {
                Set(() => SelectedSubtitle, ref _selectedSubtitle, value);
                Messenger.Default.Send(new SelectedSubtitleMessage(value));
            }
        }

        #endregion

        #region Property -> CancellationDownloadingSubtitlesToken

        /// <summary>
        /// Token to cancel downloading subtitles
        /// </summary>
        private CancellationTokenSource CancellationDownloadingSubtitlesToken { get; set; }

        #endregion

        #endregion

        public SubtitlesViewModel(MovieFull movie)
        {
            ApiService = SimpleIoc.Default.GetInstance<IApiService>();

            CancellationDownloadingSubtitlesToken = new CancellationTokenSource();

            Task.Run(async () =>
            {
                Subtitles = await GetSubtitlesAsync(movie);
            });
        }

        #region Method -> GetSubtitlesAsync
        /// <summary>
        /// Get the movie's subtitles
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <returns></returns>
        private async Task<ObservableCollection<Subtitle>> GetSubtitlesAsync(MovieFull movie)
        {
            return await ApiService.GetSubtitlesAsync(movie, CancellationDownloadingSubtitlesToken.Token);
        }
        #endregion

        #region Method -> StopDownloadingSubtitles

        /// <summary>
        /// Stop downloading subtitles
        /// </summary>
        public void StopDownloadingSubtitles()
        {
            CancellationDownloadingSubtitlesToken?.Cancel(true);
            CancellationDownloadingSubtitlesToken?.Dispose();
            CancellationDownloadingSubtitlesToken = new CancellationTokenSource();
        }

        #endregion

        public override void Cleanup()
        {
            StopDownloadingSubtitles();
            base.Cleanup();
        }
    }
}