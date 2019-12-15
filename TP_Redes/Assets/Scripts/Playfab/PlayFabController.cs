using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject _friendPanel;

    private void DisplayPlayFabError(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
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
        PlayFabClientAPI.AddFriend(request, result => { Debug.Log("Friend added successfully!"); }, DisplayPlayFabError);
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
                ListingPrefab tempListing = listing.GetComponent<ListingPrefab>();
                tempListing.playerNameText.text = f.TitleDisplayName;
            }
        }
        _myFriends = friendsCache;
    }
    
    public void RunWaitFunction()
    {
        StartCoroutine(WaitForFriend());
    }
    
    private IEnumerator WaitForFriend()
    {
        yield return new WaitForSeconds(2);
        GetFriends();
    }
    
    public void InputFriendID(string id)
    {
        _friendSearch = id;
    }
    
    public void SubmitFriendRequest()
    {
        AddFriend(FriendIdType.PlayFabId, _friendSearch);
    }

    public void OpenCloseFriends()
    {
        _friendPanel.SetActive(!_friendPanel.activeInHierarchy);
    }
}
