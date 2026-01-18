using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- SETTINGS ---
    [Header("Form Stats")]
    public float swordSpeed = 8f;
    public float swordJump = 12f;
    
    public float tankSpeed = 4f;
    public float tankJump = 5f;

    // --- ASSIGN IN INSPECTOR ---
    [Header("Forms")]
    public GameObject swordForm; 
    public GameObject tankForm; 

    // --- PRIVATE VARIABLES ---
    private Rigidbody2D currentRb; 
    private bool isSwordActive = true; 
    private float knockbackTimer = 0f; // <--- NEW VARIABLE
    
    // Add a reference to the health script
    private Health myHealth;

    void Start()
    {
        // 1. SETUP INITIAL STATE
        swordForm.SetActive(true);
        tankForm.SetActive(false);
        currentRb = swordForm.GetComponent<Rigidbody2D>();
        myHealth = GetComponent<Health>();
    }

    // --- NEW FUNCTION: CALLED BY ATTACK SCRIPT ---
    public void ApplyKnockback(Vector2 force, float duration)
    {
        // 1. Set the timer so inputs are ignored
        knockbackTimer = duration;

        // 2. Apply the force instantly
        currentRb.linearVelocity = force;
    }

    void Update()
    {
        // --- 0. CHECK KNOCKBACK (NEW) ---
        // If we are being knocked back, count down and SKIP movement code
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            return; // STOP HERE. Do not let inputs overwrite velocity.
        }

        // 1. DETERMINE STATS
        float activeSpeed = isSwordActive ? swordSpeed : tankSpeed;
        float activeJump = isSwordActive ? swordJump : tankJump;

        // 2. CHECK BLOCKING (Tank only)
        if (!isSwordActive && Input.GetKey(KeyCode.K))
        {
            myHealth.isBlocking = true;
            currentRb.linearVelocity = new Vector2(0, currentRb.linearVelocity.y);
            tankForm.GetComponent<SpriteRenderer>().color = Color.cyan;
            return; 
        }
        else
        {
            if(myHealth != null) myHealth.isBlocking = false;
            if(!isSwordActive) tankForm.GetComponent<SpriteRenderer>().color = Color.blue; 
        }

        // 3. INPUTS & SWITCHING
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchForm();
        }

        // 4. MOVEMENT
        float moveInput = Input.GetAxisRaw("Horizontal");
        currentRb.linearVelocity = new Vector2(moveInput * activeSpeed, currentRb.linearVelocity.y);

        if (moveInput != 0)
        {
            // Flip the player so the Attack Script knows we are looking Left
            currentRb.transform.localScale = new Vector3(moveInput, 1, 1);
        }

        // 5. JUMPING
        if (Input.GetButtonDown("Jump") && Mathf.Abs(currentRb.linearVelocity.y) < 0.01f)
        {
            currentRb.AddForce(Vector2.up * activeJump, ForceMode2D.Impulse);
        }
    }

    void SwitchForm()
    {
        Vector3 currentPos = currentRb.transform.position;
        Vector2 savedVelocity = currentRb.linearVelocity;

        if (isSwordActive)
        {
            swordForm.SetActive(false);
            tankForm.SetActive(true);
            tankForm.transform.position = currentPos; 
            currentRb = tankForm.GetComponent<Rigidbody2D>(); 
        }
        else
        {
            tankForm.SetActive(false);
            swordForm.SetActive(true);
            swordForm.transform.position = currentPos;
            currentRb = swordForm.GetComponent<Rigidbody2D>();
        }

        currentRb.linearVelocity = savedVelocity;
        isSwordActive = !isSwordActive;
    }
}