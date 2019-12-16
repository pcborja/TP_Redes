using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabController : MonoBehaviour
{
    public Transform friendsScrollView;
    public enum FriendIdType { PlayFabId, Username, Email, DisplayName }
    public GameObject listingPrefab;
    
    private List<FriendInfo> _myFriends;
    private string _friendSearch;
    private NetworkManager _networkManager;
    [SerializeField] private GameObject friendPanel;

    public void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
    }
    
    public void GetFriends()
    {
        PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest
        {
            IncludeSteamFriends = false,
            IncludeFacebookFriends = false
        }, result =>
        {
            _myFriends = result.Friends;
            DisplayFriends(_myFriends);
        }, DisplayPlayFabError);
    }
    
    public void AddFriend(FriendIdType idType, string friendId)
    {
        var request = new AddFriendRequest();
        switch (idType)
        {
            case FriendIdType.PlayFabId:
                request.FriendPlayFabId = friendId;
                break;
            case FriendIdType.Username:
                request.FriendUsername = friendId;
                break;
            case FriendIdType.Email:
                request.FriendEmail = friendId;
                break;
            case FriendIdType.DisplayName:
                request.FriendTitleDisplayName = friendId;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(idType), idType, null);
        }
        PlayFabClientAPI.AddFriend(request, result => { AddFriendMessage(true); }, DisplayPlayFabError);
    }
    
    public void DisplayFriends(List<FriendInfo> friendsCache)
    {
        foreach (var f in friendsCache)
        {
            var isFound = false;
            if (_myFriends != null)
            {
                foreach (var g in _myFriends)
                {
                    if (f.FriendPlayFabId == g.FriendPlayFabId)
                        isFound = true;
                }
            }
            
            if (isFound == false)
            {
                var listing = Instantiate(listingPrefab, friendsScrollView);
                var tempListing = listing.GetComponent<ListingPrefab>();
                tempListing.playerNameText.text = f.Username;
                
                var currentTime = DateTime.UtcNow;
                var breakDuration = TimeSpan.FromMinutes(5);
                tempListing.SetPlayerStatus(!(currentTime - f.Profile.LastLogin > breakDuration));
            }
        }
        _myFriends = friendsCache;
    }
    
    public void InputFriendID(string id)
    {
        _friendSearch = id;
    }
    
    public void SubmitFriendRequest()
    {
        AddFriend(FriendIdType.Username, _friendSearch);
    }

    public void OpenCloseFriends()
    {
        friendPanel.SetActive(!friendPanel.activeInHierarchy);
    }

    private void DisplayPlayFabError(PlayFabError error)
    {
        AddFriendMessage(false, error.GenerateErrorReport());
    }
    
    private void AddFriendMessage(bool success, string message = "")
    {
        if (success)
            _networkManager.ShowMessage(Constants.MessageTypes.Success, "Successfully added new friend", 5);
        else
            _networkManager.ShowMessage(Constants.MessageTypes.Error, message, 5);
    }
}
