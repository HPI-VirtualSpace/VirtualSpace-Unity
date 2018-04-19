using System.Collections;
using System.Collections.Generic;
using Games.SpaceInvaders.Scripts;
using UnityEngine;

public class SpaceInvadersUfoShoot : MonoBehaviour
{
    public SpaceInvadersGotHit GotHit;
    public SpaceInvadersEnemyShot EnemyShot;
    public float ShotInterval = 2f;
    public float ShotIntervalRandomAdd = 2f;
    [HideInInspector]
    public float FireRateFactor = 1f;

    private float _shootAgain;
    public bool Shoot;

	void Update () {

        if (Time.time > _shootAgain && Shoot && !GotHit.IsDefeated())
        {
            //shoot
            _shootAgain = Time.time + (Random.Range(0f, ShotIntervalRandomAdd) + ShotInterval) * FireRateFactor;
            var shot = Instantiate(EnemyShot);
            shot.ValidPosition = EnemyShot.transform.position;
            var pos = transform.position;
            pos.y = 0f;
            shot.transform.position = pos;
            shot.transform.forward = transform.up;
            shot.transform.SetParent(null);
        }
    }
}
