using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabController : MonoBehaviour
{
    public Transform friendsScrollView;
    public enum FriendIdType { PlayFabId, Username, Email, DisplayName }
    public GameObject listingPrefab;
    public InputField addFriendInput;
    
    private List<FriendInfo> _myFriends;
    private string _friendSearch;
    private NetworkManager _networkManager;
    private List<ListingPrefab> _instantiatedFriendsInfo = new List<ListingPrefab>();
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
        PlayFabClientAPI.AddFriend(request, result => 
        { 
            GenerateResultMessage(true, "Successfully added new friend");
            GetFriends();
        }, DisplayPlayFabError);
    }
    
    public void DisplayFriends(List<FriendInfo> friendsCache)
    {
        foreach (var f in friendsCache)
        {
            var isFound = false;
            if (_myFriends != null)
            {
                foreach (var g in _instantiatedFriendsInfo)
                {
                    if (f.Username == g.playerNameText.text)
                        isFound = true;
                }
            }
            
            if (!isFound)
            {
                var listing = Instantiate(listingPrefab, friendsScrollView);
                var tempListing = listing.GetComponent<ListingPrefab>();
                var currentTime = DateTime.UtcNow;
                var breakDuration = TimeSpan.FromMinutes(5);

                tempListing.friendId = f.FriendPlayFabId;
                tempListing.playerNameText.text = f.Username;
                
                if (f.Profile != null)
                    tempListing.SetPlayerStatus(!(currentTime - f.Profile.LastLogin > breakDuration));
                
                tempListing.removeButton.onClick.AddListener(() => RemoveFriend(f));
                
                _instantiatedFriendsInfo.Add(tempListing);
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
        _networkManager.UpdateInputField(addFriendInput);
    }

    public void OpenCloseFriends()
    {
        friendPanel.SetActive(!friendPanel.activeInHierarchy);
    }

    private void DisplayPlayFabError(PlayFabError error)
    {
        GenerateResultMessage(false, "", error.GenerateErrorReport());
    }
    
    private void GenerateResultMessage(bool success, string successMessage, string message = "")
    {
        if (success)
            _networkManager.ShowMessage(Constants.MessageTypes.Success, successMessage, 5);
        else
            _networkManager.ShowMessage(Constants.MessageTypes.Error, message, 5);
    }

    private void RemoveFriend(FriendInfo friendInfo) 
    {
        PlayFabClientAPI.RemoveFriend(new RemoveFriendRequest 
        { FriendPlayFabId = friendInfo.FriendPlayFabId },
            result =>
            {
                ClearFriend(friendInfo);
                GetFriends();
                GenerateResultMessage(true, "Successfully removed friend");
            }, 
            DisplayPlayFabError);
    }

    private void ClearFriend(FriendInfo friendInfo)
    {
        _myFriends.Remove(friendInfo);
        var friendToRemove = _instantiatedFriendsInfo.FirstOrDefault(x => x.friendId == friendInfo.FriendPlayFabId);

        if (friendToRemove != default)
        {
            _instantiatedFriendsInfo.Remove(friendToRemove);
            Destroy(friendToRemove.gameObject);
        }
    }
}
