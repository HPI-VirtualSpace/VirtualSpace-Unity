using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpace.Shared;

public class AreaDisplayer : MonoBehaviour
{
    public UnityPolygon PolygonTemplate;
    private Dictionary<int, UnityPolygon> _userPolygons = new Dictionary<int, UnityPolygon>();
    private Dictionary<int, List<UnityPolygon>> _fillerPolygons = new Dictionary<int, List<UnityPolygon>>();
    private List<int> _updated = new List<int>();
    public Users Users;
    public float SafetyOffset = -.3f;
    public float LookAhead = 2f;
    [SerializeField]
    private string CurrentState;

    private long _highestTransitionId = -1;
    void Update() {
        long now = VirtualSpaceTime.CurrentTurn;
        long lookAheadTurns = VirtualSpaceTime.ConvertSecondsToTurns(LookAhead);

        var events =
            EventMap.Instance
                .GetEventsForTurnExpandingTransitions(now, now + lookAheadTurns,
                    new[] { VirtualSpace.Shared.EventType.Area }, new[] { IncentiveType.Recommended })
                .Select(event_ => (TimedArea)event_);

        var userIdsToAreas = new Dictionary<int, List<TimedArea>>();

        foreach (var event_ in events)
        {
            if (!userIdsToAreas.ContainsKey(event_.PlayerId))
            {
                userIdsToAreas[event_.PlayerId] = new List<TimedArea>();
            }

            userIdsToAreas[event_.PlayerId].Add(event_);
        }

        userIdsToAreas.Clear();
        FindActiveAreas(now, lookAheadTurns, userIdsToAreas);
        
        _updated.Clear();
        foreach (var pair in userIdsToAreas)
	    {
            var userId = pair.Key;
            var areasToDisplay = pair.Value;

            var user = Users.GetUser(userId);

            // can't simply use transitions since we want to look into the future
            
	        if (!_userPolygons.ContainsKey(userId))
            {
                _userPolygons[userId] = CreateUnityPolygon(user);
            }
            
            var nowArea = areasToDisplay.Find(area => area.IsActiveAt(now));

	        if (nowArea != null)
            {
                CurrentState = nowArea.DisplayName;

                var nowPolygon = nowArea.Area;
                var clipperResult = ClipperUtility.OffsetPolygonForSafety(nowPolygon, SafetyOffset);
                if (clipperResult.Count == 0)
                {
                    Debug.LogWarning("Offsetting cancelled polygon " + userId + " at " + now);
                    continue;
                }

                nowPolygon = clipperResult.First();
                Vector2[] unityPoints = TransformToUnityCoordinates(nowPolygon);
                _userPolygons[userId].Points = unityPoints;
                
                //if (!_fillerPolygons.ContainsKey(userId))
                //{
                //    _fillerPolygons[userId] = new List<UnityPolygon>();
                //    for (int i = 0; i < 5; i++)
                //    {
                //        _fillerPolygons[userId].Add(CreateUnityPolygon(user));
                //    }
                //}

                //long turnsPerBreak = lookAheadTurns / 5;
                //for (int i = 0; i < 5; i++)
                //{
                //    long futureTurn = now + turnsPerBreak * i;
                //    TimedArea futureAreaEvent = areasToDisplay.Find(area => area.IsActiveAt(futureTurn));
                //    Polygon futureArea = null;

                //    if (futureAreaEvent != null)
                //    {
                //        var thenPolygon = futureAreaEvent.Area;
                //        clipperResult = ClipperUtility.Intersection(nowPolygon, thenPolygon);
                //        if (clipperResult.Count != 0) futureArea = clipperResult.First();
                //    }
                        
                //    if (futureArea != null)
                //    {
                //        var displayPolygon = futureArea;
                //        _fillerPolygons[userId][i].Points = TransformToUnityCoordinates(displayPolygon);
                //    } else
                //    {
                //        _fillerPolygons[userId][i].Points = new Vector2[0];
                //    }
                //}

                _updated.Add(userId);
            }
	        else
	        {
                Debug.LogWarning("Didn't find for " + now);
            }
	    }

        foreach (var user in Users.All)
        {
            if (!_updated.Contains(user.Id))
            {
                if (_userPolygons.ContainsKey(user.Id))
                {
                    Destroy(_userPolygons[user.Id].gameObject);
                    _userPolygons.Remove(user.Id);
                }

                if (_fillerPolygons.ContainsKey(user.Id))
                {
                    _fillerPolygons[user.Id].ForEach(polygon => Destroy(polygon.gameObject));
                    _fillerPolygons.Remove(user.Id);
                }
            }
        }
    }

    private void FindActiveAreas(long now, long lookAheadTurns, Dictionary<int, List<TimedArea>> userIdsToAreas)
    {
        var transitions = EventMap.Instance.GetEventsForFromTo(now, now + lookAheadTurns)
                        .Select(event_ => (Transition)event_);
        foreach (var transition in transitions)
        {
            if (_highestTransitionId < transition.Id)
            {
                Debug.Log("Active from " + transition.TurnStart + " to " + transition.TurnEnd);

                _highestTransitionId = transition.Id;
            }

            foreach (var frame in transition.GetActiveBetween(now, now + lookAheadTurns))
            {
                if (!userIdsToAreas.ContainsKey(transition.PlayerId))
                {
                    userIdsToAreas[transition.PlayerId] = new List<TimedArea>();
                }

                userIdsToAreas[transition.PlayerId].Add(frame.Area);
            }
        }
    }

    private Vector2[] TransformToUnityCoordinates(Polygon nowPolygon)
    {
        return nowPolygon.Points.Select(vector =>
        {
            //var vec3 = new Vector3((float)vector.X, (float)vector.Z, -1);
            //var transformedVec3 = transform.TransformPoint(vec3);
            //var vec2 = new Vector2(transformedVec3.x, transformedVec3.y);
            //return vec2;
            return vector.ToVector2();
        }).ToArray();
    }

    private UnityPolygon CreateUnityPolygon(User user)
    {
        var unityPolygon = Instantiate(PolygonTemplate);
        unityPolygon.transform.parent = transform;
        unityPolygon.Material = user.AreaMaterial;
        return unityPolygon;
    }
}
