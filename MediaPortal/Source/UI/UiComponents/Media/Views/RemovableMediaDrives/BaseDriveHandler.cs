#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common;
using System.Linq;
using System;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Base class for an <see cref="IRemovableDriveHandler"/>. Provides a default handling for the <see cref="VolumeLabel"/> property.
  /// </summary>
  public abstract class BaseDriveHandler : IRemovableDriveHandler
  {
    #region Protected fields

    protected DriveInfo _driveInfo;
    protected string _volumeLabel;

    #endregion

    protected BaseDriveHandler(DriveInfo driveInfo)
    {
      _driveInfo = driveInfo;
      _volumeLabel = null;
      if (_driveInfo.IsReady)
        try
        {
          _volumeLabel = _driveInfo.VolumeLabel;
        }
        catch (IOException)
        {
        }
      _volumeLabel = _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(_volumeLabel) ? string.Empty : ("(" + _volumeLabel + ")"));
    }

    /// <summary>
    /// Returns a string of the form <c>"D: (Videos)"</c> or <c>"D:"</c>.
    /// </summary>
    public virtual string VolumeLabel
    {
      get { return _volumeLabel; }
    }

    public abstract IList<MediaItem> MediaItems { get; }
    public abstract IList<ViewSpecification> SubViewSpecifications { get; }
    public abstract IEnumerable<MediaItem> GetAllMediaItems();
    public static MediaItem FindStub(DriveInfo driveInfo, MediaItem mediaItem)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return mediaItem;

      IList<MediaItem> existingItems;
      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      //Try merge handlers
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
      {
        IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = mediaItem.Aspects;
        // Extracted aspects must contain all of mergeHandler.MergeableAspects
        if (mergeHandler.MergeableAspects.All(g => extractedAspects.Keys.Contains(g)))
        {
          IFilter filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, 
            mergeHandler.GetSearchFilter(extractedAspects), 
            new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
          if (filter != null)
          {
            existingItems = cd.Search(new MediaItemQuery(mergeHandler.MergeableAspects, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, true);
            foreach (MediaItem existingItem in existingItems)
            {
              if (mergeHandler.TryMatch(extractedAspects, existingItem.Aspects))
              {
                mergeHandler.TryMerge(extractedAspects, existingItem.Aspects);
                return existingItem;
              }
            }
          }
        }
      }

      //Try stub label
      existingItems = cd.Search(new MediaItemQuery(Consts.NECESSARY_BROWSING_MIAS, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, 
          new RelationalFilter(MediaAspect.ATTR_STUB_LABEL, RelationalOperator.EQ, driveInfo.VolumeLabel),
          new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true))), false, userProfile, true);
      if (existingItems != null && existingItems.Count == 1)
      {
        MediaItem existingItem = existingItems.First();
        if (existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        {
          int newResIndex = 0;
          foreach (MediaItemAspect mia in existingItem.Aspects[ProviderResourceAspect.ASPECT_ID])
          {
            if (newResIndex <= mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))
              newResIndex = mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) + 1;
          }
          foreach (MediaItemAspect mia in mediaItem.Aspects[ProviderResourceAspect.ASPECT_ID])
          {
            mia.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, newResIndex);
            existingItem.Aspects[ProviderResourceAspect.ASPECT_ID].Add(mia);
            newResIndex++;
          }
        }
        return existingItem;
      }

      return mediaItem;
    }
  }
}
