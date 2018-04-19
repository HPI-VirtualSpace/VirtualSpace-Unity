using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpaceInvadersEnemyRow : MonoBehaviour
{
    public List<SpaceInvadersEnemy> Enemies;
    public float LeftRightDistance = 2f;
    public float TimeForDistance = 4f;
    public float FireRateSlow = 5f;
    [HideInInspector]
    public int Points;
    [HideInInspector]
    public bool Done;
    public int PointsPerEnemy;
    public float UpdateRate = 1f;
    
    private Vector3 _startPos;
    private int _startCount;
    private float _nextUpdate;

    // Use this for initialization
    void Start ()
    {
        _startPos = transform.position;
        //EnemyShot.gameObject.SetActive(false);
        _startCount = Enemies.Count;
    }
	
	// Update is called once per frame
	void Update () {

        if(Time.time > _nextUpdate)
        {
            _nextUpdate = Time.time + UpdateRate;
            UpdatePosition();
            UpdateFireRate();
        }
	}

    private void UpdatePosition()
    {
        //handle position of enemies
        transform.position = _startPos +
                             (Mathf.Abs((Time.time % TimeForDistance) / TimeForDistance - 0.5f) * 2f - 0.5f) * 2f *
                             LeftRightDistance * transform.forward;

    }

    private void UpdateFireRate()
    {
        var angle = Vector3.Angle(Camera.main.transform.forward, transform.up);
        var firerate = angle < 90f ? 1f : FireRateSlow;
        foreach(var enemy in Enemies)
        {
            enemy.FireRateFactor = firerate;
        }
    }

    public void DeregisterEnemy(SpaceInvadersEnemy e)
    {
        //handle dead enemies
        Enemies.Remove(e);
        Destroy(e.gameObject);

        //handle text
        var enemyNumber = Enemies.Where(en => !en.Defeated()).ToList().Count;
        Points = (_startCount - enemyNumber) * PointsPerEnemy;
        Done = enemyNumber == 0;
    }
}
