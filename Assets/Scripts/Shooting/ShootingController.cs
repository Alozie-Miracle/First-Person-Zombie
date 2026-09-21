using UnityEngine;

public class ShootingController : MonoBehaviour
{
    public Transform firePoint; // main camera
    public float fireRate = 0.1f;
    public float fireRange = 10f;

    private float nextFireTime = 0f;

    PlayerInputHandler input;

    void Awake()
    {
        input = FindAnyObjectByType<PlayerInputHandler>();
    }

    void Update()
    {
        if (input.isShooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }


    void Shoot()
    {
        RaycastHit hit;

        // this code sends out raycast, from the camera forward direction and reads value at a fixed range (fire range)
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
        {
            Debug.Log(hit.transform.name);
        }

    }
}
