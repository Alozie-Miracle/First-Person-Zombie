using UnityEngine;

public class ShootingController : MonoBehaviour
{
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");

    private int currentAmmo;
    private bool isReloading = false;
    private float nextFireTime = 0f;

    
    public Animator animator;
    public Transform firePoint; // main camera
    public float fireRate = 0.1f;
    public float fireRange = 10f;
    public int maxAmmo = 40;
    public float reloadTime = 1.5f;

    PlayerInputHandler input;

    void Start()
    {
        input = FindAnyObjectByType<PlayerInputHandler>();
        currentAmmo = maxAmmo;
    }

    void Update()
    {

        if (isReloading) return;


        if (input.isShooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        } else
        {
            animator.SetBool(ShootHash, false);
        }

        if (input.isReloading && currentAmmo < maxAmmo)
        {
            Reload();
        }
    }


    void Shoot()
    {
        if (currentAmmo > 0)
        {

            // this code sends out raycast, from the camera forward direction and reads value at a fixed range (fire range)
            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, fireRange))
            {
                Debug.Log(hit.transform.name);

                // TODO: apple damage to zombie
            }

            animator.SetBool(ShootHash, true);
            currentAmmo--;
        } 
        else
        {
            // Reload
            Reload();
        } 

    }

    void Reload()
    {
        if (!isReloading && currentAmmo < maxAmmo)
        {
            // play reload animation
            animator.SetTrigger(ReloadHash);
            isReloading = true;
            // play reload sound
            Invoke(nameof(FinishReloading), reloadTime);
        }
    }

    void FinishReloading()
    {
        currentAmmo = maxAmmo;
        isReloading = false;

        // reset reload animation
        animator.ResetTrigger(ReloadHash);
    }
}
