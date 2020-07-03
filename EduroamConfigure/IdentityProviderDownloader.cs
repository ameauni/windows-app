﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using System.Device.Location;

namespace EduroamConfigure
{
    public class IdentityProviderDownloader
    {
        // constants
        private static string GeoApiUrl = "https://geo.geteduroam.app/geoip";
        private static string ProviderApiUrl = "https://discovery.geteduroam.no/v1/discovery.json";

        // state
        public List<IdentityProvider> Providers;
        private GeoCoordinateWatcher GeoWatcher;

        /// <summary>
        /// The constructor for this class.
        /// Will download the list of all providers
        /// </summary>
        public IdentityProviderDownloader()
        {
            GeoWatcher = new GeoCoordinateWatcher();
            GeoWatcher.TryStart(false, TimeSpan.FromMilliseconds(3000));
            Providers = DownloadAllIdProviders();
        }

        /// <summary>
        /// Will return the current coordinates of the users.
        /// It may download them if not cached
        /// </summary>
        private GeoCoordinate GetCoordinates()
        {
            if (!GeoWatcher.Position.Location.IsUnknown)
            {
                return GeoWatcher.Position.Location;
            } 
            return DownloadCoordinates();
        }


        /// <summary>
        /// Fetches discovery data from geteduroam
        /// </summary>
        /// <returns>DiscoveryApi object with the ata fetched</returns>
        /// <exception cref="EduroamAppUserError">description</exception>
        private static DiscoveryApi DownloadDiscoveryApi()
        {
            try
            {
                // downloads json file as string
                string apiJson = DownloadUrlAsString(ProviderApiUrl);
                // gets api instance from json
                DiscoveryApi apiInstance = JsonConvert.DeserializeObject<DiscoveryApi>(apiJson);
                return apiInstance;
            }
            catch (WebException ex)
            {
                throw new EduroamAppUserError("providers download error", WebExceptionToString(ex));
            }
            catch (JsonReaderException ex)
            {
                throw new EduroamAppUserError("providers download error", JsonExceptionToString(ex));
            }
        }

        private static Location GetCurrentLocationFromGeoApi()
        {
            try
            {
                string apiJson = DownloadUrlAsString(GeoApiUrl);
                Location apiInstance = JsonConvert.DeserializeObject<Location>(apiJson);
                return apiInstance;
            }
            catch (WebException ex)
            {
                throw new EduroamAppUserError("location download error", WebExceptionToString(ex));
            }
            catch (JsonReaderException ex)
            {
                throw new EduroamAppUserError("location download error", JsonExceptionToString(ex));
            }
        }

        /// <summary>
        /// Downloads a list of all eduroam institutions
        /// </summary>
        /// <exception cref="EduroamAppUserError">description</exception>
        private static List<IdentityProvider> DownloadAllIdProviders()
        {
            return DownloadDiscoveryApi().Instances;
        }

        /// <summary>
        /// Gets all profiles associated with a identity provider ID.
        /// </summary>
        /// <returns>identity provider profile object containing all profiles for given provider</returns>
        /// <exception cref="EduroamAppUserError">description</exception>
        public List<IdentityProviderProfile> GetIdentityProviderProfiles(int idProviderId)
        {
            return Providers.Where(p => p.cat_idp == idProviderId).First().Profiles;
        }

        /// <summary>
        /// Returns the n closest providers
        /// </summary>
        /// <param name="limit">number of providers to return</param>
        public List<IdentityProvider> GetClosestProviders(int limit)
        {
            // find all providers in current country
            string closestCountryCode = GetCurrentLocationFromGeoApi().Country;
            var userCoords = GetCoordinates();

            // sort and return n closest
            return Providers
                .Where(p => p.Country == closestCountryCode)
                .OrderBy(p => userCoords.GetDistanceTo(p.GetClosestGeoCoordinate(userCoords)))
                .Take(limit)
                .ToList();
        }

        private static GeoCoordinate DownloadCoordinates()
        {
            return GetCurrentLocationFromGeoApi().Geo.GeoCoordinate;
        }

        /// <summary>
        /// Gets download link for EAP config from json and downloads it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EduroamAppUserError">description</exception>
        public string GetEapConfigString(string profileId)
        {
            // adds profile ID to url containing json file, which in turn contains url to EAP config file download
            // gets url to EAP config file download from GenerateEapConfig object
            string endpoint = GetProfileFromId(profileId).eapconfig_endpoint;

            // downloads and returns eap config file as string
            try
            {
                return DownloadUrlAsString(endpoint);
            }
            catch (WebException ex)
            {
                throw new EduroamAppUserError("WebException", WebExceptionToString(ex));
            }
        }

        public IdentityProviderProfile GetProfileFromId(string profileId)
        {
            foreach (IdentityProvider provider in Providers)
            {
                foreach (IdentityProviderProfile profile in provider.Profiles)
                {
                    if (profile.Id == profileId)
                    {
                        return profile;
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Gets a json file as string from url.
        /// </summary>
        /// <param name="url">Url containing json file.</param>
        /// <returns>Json string.</returns>
        private static string DownloadUrlAsString(string url)
        {
            // download json file from url as string
            using var client = new WebClient();
            client.Encoding = Encoding.UTF8;

            return client.DownloadString(url);
        }


        /// <summary>
        /// Produces error string for web exceptions that occur if user loses an existing connection.
        /// </summary>
        /// <param name="ex">WebException.</param>
        /// <returns>String containing error message meant for user to read.</returns>
        private static string WebExceptionToString(WebException ex)
        {
            return
                "Couldn't connect to the server.\n\n" + 
                "Make sure that you are connected to the internet, then try again.\n" +
                "Exception: " + ex.Message;
        }


        /// <summary>
        /// Produces error string for exceptions related to deserializing JSON files and corrupted XML files.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns>String containing error message meant for user to read.</returns>
        private static string JsonExceptionToString(Exception ex)
        {
            return
                "The selected institution or profile is not supported. " +
                "Please select a different institution or profile.\n" +
                "Exception: " + ex.Message;
        }

    }
       
}
