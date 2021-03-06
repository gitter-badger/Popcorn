﻿using Popcorn.Models.Movie.Full;
using System.Threading.Tasks;

namespace Popcorn.ViewModels.Subtitles
{
    public interface ISubtitlesViewModel
    {
        /// <summary>
        /// Get the movie's subtitles
        /// </summary>
        /// <param name="movie">The movie</param>
        Task LoadSubtitlesAsync(MovieFull movie);

        /// <summary>
        /// Stop downloading subtitles and clear movie
        /// </summary>
        void ClearSubtitles();

        /// <summary>
        /// Cleanup resources
        /// </summary>
        void Cleanup();
    }
}
