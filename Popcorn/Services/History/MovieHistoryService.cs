﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Popcorn.Entity;
using Popcorn.Entity.Cast;
using Popcorn.Entity.Movie;
using Popcorn.Models.Genre;
using MovieFull = Popcorn.Models.Movie.Full.MovieFull;
using MovieShort = Popcorn.Models.Movie.Short.MovieShort;
using Torrent = Popcorn.Models.Torrent.Torrent;

namespace Popcorn.Services.History
{
    /// <summary>
    /// Services used to interacts with movie history
    /// </summary>
    public class MovieHistoryService : IMovieHistoryService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Retrieve from database and set the IsFavorite and HasBeenSeen properties of each movie in params, 
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        public async Task ComputeMovieHistoryAsync(IEnumerable<MovieShort> movies)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var history = await context.MovieHistory.FirstOrDefaultAsync();
                    if (history == null)
                    {
                        await CreateMovieHistoryAsync();
                        history = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    foreach (var movie in movies)
                    {
                        var entityMovie = history.MoviesShort.FirstOrDefault(p => p.MovieId == movie.Id);
                        if (entityMovie == null) continue;
                        movie.IsFavorite = entityMovie.IsFavorite;
                        movie.HasBeenSeen = entityMovie.HasBeenSeen;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"ComputeMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"ComputeMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get the favorites movies
        /// </summary>
        /// <param name="genre">The genre of the movies</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Favorites movies</returns>
        public async Task<IEnumerable<MovieShort>> GetFavoritesMoviesAsync(MovieGenre genre, double ratingFilter)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieShort>();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (genre != null)
                    {
                        movies.AddRange(movieHistory.MoviesShort.Where(
                            p =>
                                p.IsFavorite && p.Genres.Any(g => g.Name == genre.EnglishName) &&
                                p.Rating >= ratingFilter)
                            .Select(MovieShortFromEntityToModel));
                    }
                    else
                    {
                        movies.AddRange(movieHistory.MoviesShort.Where(
                            p => p.IsFavorite)
                            .Select(MovieShortFromEntityToModel));
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetFavoritesMoviesIdAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetFavoritesMoviesIdAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Get the seen movies
        /// </summary>
        /// <returns>Seen movies</returns>
        /// <param name="genre">The genre of the movies</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        public async Task<IEnumerable<MovieShort>> GetSeenMoviesAsync(MovieGenre genre, double ratingFilter)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieShort>();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (genre != null)
                    {
                        movies.AddRange(movieHistory.MoviesShort.Where(
                            p =>
                                p.HasBeenSeen && p.Genres.Any(g => g.Name == genre.EnglishName) &&
                                p.Rating >= ratingFilter)
                            .Select(MovieShortFromEntityToModel));
                    }
                    else
                    {
                        movies.AddRange(movieHistory.MoviesShort.Where(
                            p => p.HasBeenSeen)
                            .Select(MovieShortFromEntityToModel));
                    }
                }
            }
            catch (Exception exception)
            {

                Logger.Error(
                    $"GetSeenMoviesIdAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetSeenMoviesIdAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Set the movie as favorite
        /// </summary>
        /// <param name="movie">Favorite movie</param>
        public async Task SetFavoriteMovieAsync(MovieShort movie)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                    {
                        await CreateMovieHistoryAsync();
                        movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    if (movieHistory.MoviesShort == null)
                    {
                        movieHistory.MoviesShort = new List<Entity.Movie.MovieShort>
                        {
                            MovieShortFromModelToEntity(movie)
                        };

                        context.MovieHistory.AddOrUpdate(movieHistory);
                    }
                    else
                    {
                        var movieShort = movieHistory.MoviesShort.FirstOrDefault(p => p.MovieId == movie.Id);
                        if (movieShort == null)
                        {
                            movieHistory.MoviesShort.Add(MovieShortFromModelToEntity(movie));
                        }
                        else
                        {
                            movieShort.IsFavorite = movie.IsFavorite;
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetFavoriteMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetFavoriteMovieAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set a movie as seen
        /// </summary>
        /// <param name="movie">Seen movie</param>
        public async Task SetHasBeenSeenMovieAsync(MovieFull movie)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                    {
                        await CreateMovieHistoryAsync();
                        movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    if (movieHistory.MoviesFull == null)
                    {
                        movieHistory.MoviesFull = new List<Entity.Movie.MovieFull>
                        {
                            MovieFullFromModelToEntity(movie)
                        };

                        context.MovieHistory.AddOrUpdate(movieHistory);
                    }
                    else
                    {
                        var movieFull = movieHistory.MoviesFull.FirstOrDefault(p => p.MovieId == movie.Id);
                        if (movieFull == null)
                        {
                            movieHistory.MoviesFull.Add(MovieFullFromModelToEntity(movie));
                        }
                        else
                        {
                            movieFull.HasBeenSeen = movie.HasBeenSeen;
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetHasBeenSeenMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetHasBeenSeenMovieAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Scaffold UserData Table on database if empty
        /// </summary>
        private static async Task CreateMovieHistoryAsync()
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var userData = await context.MovieHistory.FirstOrDefaultAsync();
                    if (userData == null)
                    {
                        context.MovieHistory.AddOrUpdate(new MovieHistory
                        {
                            Created = DateTime.Now,
                            MoviesShort = new List<Entity.Movie.MovieShort>(),
                            MoviesFull = new List<Entity.Movie.MovieFull>()
                        });

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"CreateMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"CreateMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Convert a short movie entity to a short movie model
        /// </summary>
        /// <param name="movie">The movie to convert</param>
        /// <returns>Short movie model</returns>
        private static MovieShort MovieShortFromEntityToModel(Entity.Movie.MovieShort movie)
        {
            var torrents = movie.Torrents.Select(torrent => new Torrent
            {
                DateUploaded = torrent.DateUploaded,
                Url = torrent.Url,
                Quality = torrent.Quality,
                DateUploadedUnix = torrent.DateUploadedMix,
                Framerate = torrent.Framerate,
                Hash = torrent.Hash,
                Peers = torrent.Peers,
                Resolution = torrent.Resolution,
                Seeds = torrent.Seeds,
                Size = torrent.Size,
                SizeBytes = torrent.SizeBytes
            });

            return new MovieShort
            {
                Language = movie.Language,
                ApiVersion = movie.ApiVersion,
                CoverImagePath = movie.CoverImagePath,
                DateUploaded = movie.DateUploaded,
                DateUploadedUnix = movie.DateUploadedUnix,
                ExecutionTime = movie.ExecutionTime,
                Genres = movie.Genres.Select(x => x.Name).ToList(),
                HasBeenSeen = movie.HasBeenSeen,
                Id = movie.MovieId,
                ImdbCode = movie.ImdbCode,
                IsFavorite = movie.IsFavorite,
                Runtime = movie.Runtime,
                RatingValue = movie.Rating,
                MpaRating = movie.MpaRating,
                Title = movie.Title,
                TitleLong = movie.TitleLong,
                Torrents = torrents.ToList(),
                MediumCoverImage = movie.MediumCoverImage,
                Url = movie.Url,
                State = movie.State,
                ServerTimezone = movie.ServerTimezone,
                ServerTime = movie.ServerTime,
                SmallCoverImage = movie.SmallCoverImage,
                Year = movie.Year
            };
        }

        /// <summary>
        /// Convert a short movie model to a short movie entity
        /// </summary>
        /// <param name="movie">The movie to convert</param>
        /// <returns>Short movie entity</returns>
        private static Entity.Movie.MovieShort MovieShortFromModelToEntity(MovieShort movie)
        {
            var torrents = movie.Torrents.Select(torrent => new Entity.Movie.Torrent
            {
                DateUploaded = torrent.DateUploaded,
                Url = torrent.Url,
                Quality = torrent.Quality,
                DateUploadedMix = torrent.DateUploadedUnix,
                Framerate = torrent.Framerate,
                Hash = torrent.Hash,
                Peers = torrent.Peers,
                Resolution = torrent.Resolution,
                Seeds = torrent.Seeds,
                Size = torrent.Size,
                SizeBytes = torrent.SizeBytes
            });

            var genres = movie.Genres.Select(genre => new Genre
            {
                Name = genre
            });

            var movieShort = new Entity.Movie.MovieShort
            {
                MovieId = movie.Id,
                IsFavorite = movie.IsFavorite,
                HasBeenSeen = movie.HasBeenSeen,
                ServerTime = movie.ServerTime,
                ServerTimezone = movie.ServerTimezone,
                SmallCoverImage = movie.SmallCoverImage,
                State = movie.State,
                Year = movie.Year,
                Language = movie.Language,
                ImdbCode = movie.ImdbCode,
                Title = movie.Title,
                Id = movie.Id,
                DateUploaded = movie.DateUploaded,
                Runtime = movie.Runtime,
                Url = movie.Url,
                TitleLong = movie.TitleLong,
                Torrents = torrents.ToList(),
                MediumCoverImage = movie.MediumCoverImage,
                Genres = genres.ToList(),
                DateUploadedUnix = movie.DateUploadedUnix,
                CoverImagePath = movie.CoverImagePath,
                MpaRating = movie.MpaRating,
                Rating = movie.RatingValue,
                ExecutionTime = movie.ExecutionTime,
                ApiVersion = movie.ApiVersion
            };

            return movieShort;
        }

        /// <summary>
        /// Convert a full movie model to a full movie entity
        /// </summary>
        /// <param name="movie">The movie to convert</param>
        /// <returns>Full movie entity</returns>
        private static Entity.Movie.MovieFull MovieFullFromModelToEntity(MovieFull movie)
        {
            var torrents = movie.Torrents.Select(torrent => new Entity.Movie.Torrent
            {
                DateUploaded = torrent.DateUploaded,
                Url = torrent.Url,
                Quality = torrent.Quality,
                DateUploadedMix = torrent.DateUploadedUnix,
                Framerate = torrent.Framerate,
                Hash = torrent.Hash,
                Peers = torrent.Peers,
                Resolution = torrent.Resolution,
                Seeds = torrent.Seeds,
                Size = torrent.Size,
                SizeBytes = torrent.SizeBytes
            });

            var genres = movie.Genres.Select(genre => new Genre
            {
                Name = genre
            });

            var images = new Images
            {
                BackgroundImage = movie.Images.BackgroundImage,
                MediumCoverImage = movie.Images.MediumCoverImage,
                SmallCoverImage = movie.Images.SmallCoverImage,
                LargeCoverImage = movie.Images.LargeCoverImage,
                LargeScreenshotImage1 = movie.Images.LargeScreenshotImage1,
                LargeScreenshotImage2 = movie.Images.LargeScreenshotImage2,
                LargeScreenshotImage3 = movie.Images.MediumScreenshotImage3,
                MediumScreenshotImage3 = movie.Images.MediumScreenshotImage3,
                MediumScreenshotImage1 = movie.Images.MediumScreenshotImage1,
                MediumScreenshotImage2 = movie.Images.MediumScreenshotImage2
            };

            var actors = movie.Actors.Select(actor => new Actor
            {
                CharacterName = actor.CharacterName,
                MediumImage = actor.MediumImage,
                Name = actor.Name,
                SmallImage = actor.SmallImage,
                SmallImagePath = actor.SmallImagePath
            });

            var directors = movie.Directors.Select(actor => new Director
            {
                MediumImage = actor.MediumImage,
                Name = actor.Name,
                SmallImage = actor.SmallImage,
                SmallImagePath = actor.SmallImagePath
            });

            var movieFull = new Entity.Movie.MovieFull
            {
                MovieId = movie.Id,
                Year = movie.Year,
                Language = movie.Language,
                ImdbCode = movie.ImdbCode,
                Title = movie.Title,
                Id = movie.Id,
                DateUploaded = movie.DateUploaded,
                Runtime = movie.Runtime,
                Url = movie.Url,
                TitleLong = movie.TitleLong,
                Torrents = torrents.ToList(),
                Genres = genres.ToList(),
                DateUploadedUnix = movie.DateUploadedUnix,
                MpaRating = movie.MpaRating,
                Rating = movie.RatingValue,
                Images = images,
                DescriptionFull = movie.DescriptionFull,
                Actors = actors.ToList(),
                Directors = directors.ToList(),
                DescriptionIntro = movie.DescriptionIntro,
                DownloadCount = movie.DownloadCount,
                LikeCount = movie.LikeCount,
                RtAudienceRating = movie.RtAudienceRating,
                RtAudienceScore = movie.RtAudienceScore,
                RtCriticsRating = movie.RtCriticsRating,
                RtCrtiticsScore = movie.RtCrtiticsScore,
                YtTrailerCode = movie.YtTrailerCode,
                HasBeenSeen = movie.HasBeenSeen,
                IsFavorite = movie.IsFavorite
            };

            return movieFull;
        }
    }
}