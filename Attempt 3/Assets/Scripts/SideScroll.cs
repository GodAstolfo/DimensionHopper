using UnityEditor.Animations;
using UnityEngine;

// SideScrollPlayerController
// Controls 2D platformer movement (left/right translation, jump, wall-jump),
// handles simple ground/wall detection, forwards parameters to an Animator,
// and manages a separate visual child (visualRoot) so the sprite/animation can be
// flipped without altering the physics/collider object.
public class SideScrollPlayerController : MonoBehaviour
{
    // Movement tuning -----------------------------------------------------
    // Horizontal movement speed (units per second when using Translate).
    public float moveSpeed = 10.0f;
    // Impulse applied for a normal jump. Applied with ForceMode2D.Impulse.
    public float jumpForce = 500.0f;

    // horizontal impulse applied during wall-jump (tweakable)
    public float wallJumpHorizontalForce = 250.0f;

                                        // Cached component references (set at Start) ---------------------------
    Rigidbody2D rb;            // physics body used for jump impulses and reading velocity
    Animator knightimation;    // animator for the visual child (if present)
    SpriteRenderer spriteRenderer; // fallback renderer when no visual child exists

    // Visual/flip handling ------------------------------------------------
    // visualRoot: optional child Transform that contains SpriteRenderer + Animator.
    // Keeping visuals in a child lets us flip/scale the visual without changing
    // the physics/collider root transform.
    public Transform visualRoot;
    // The original localScale read from visualRoot (absolute; x will be made positive).
    Vector3 visualOriginalScale = Vector3.one;
    // visualFacing is +1 when facing right, -1 when facing left.
    int visualFacing = 1; // +1 = facing right, -1 = facing left

    // An anchor transform created at the collider center. The visualRoot is
    // reparented under this anchor so flips change only localScale and do not
    // require position mirroring math that can drift over time.
    private Transform visualAnchor;

    // Collider used as the flip anchor; the visualAnchor is positioned at
    // the collider's center in local space so visuals pivot around the collider.
    Collider2D mainCollider;

    // State flags exposed for debugging/inspection ------------------------
    public bool isGrounded = false;       // true while overlapping ground at groundCheck
    public bool touchingWallLeft = false; // true when a left-side wall overlaps wallCheck1
    public bool touchingWallRight = false;// true when a right-side wall overlaps wallCheck2

    // Deferred jump flags (set in Update, consumed in FixedUpdate)
    // These avoid applying physics impulses directly from Update().
    public bool shouldJump = false;
    public bool shouldWallJump = false;
    int wallJumpDirection = 0; // +1 = push right (jump off left wall), -1 = push left

    // Ground / wall detection config -------------------------------------
    // Assign transforms positioned at the player's feet and sides for overlap checks.
    public Transform groundCheck;    // empty child at feet for ground detection
    public Transform wallCheck1;     // empty child on one side for wall detection (left)
    public Transform wallCheck2;     // empty child on other side for wall detection (right)
    // Radius used for Physics2D.OverlapCircle checks (tweak to fit collider)
    public float surfaceCheckRadius = 0.12f;
    // LayerMask specifying which layers count as ground/wall (use a "Ground" layer)
    public LayerMask groundLayer;

    [Header("Animator: rising/falling")]
    // threshold to decide when vertical velocity counts as rising/falling
    // A small tolerance prevents rapid flicker near zero velocity.
    [SerializeField] private float verticalThreshold = 0.1f;

    [Header("Visual flip scale (allows slight scale differences when facing)")]
    [Tooltip("Multiplier applied to visual original scale when facing right.")]
    [SerializeField] private float scaleWhenFacingRight = 1f;
    [Tooltip("Multiplier applied to visual original scale when facing left.")]
    [SerializeField] private float scaleWhenFacingLeft = 1f;

    // tracks the current absolute visual X scale used (prevents cumulative drift
    // when flipping if you allow different absolute multipliers for each facing)
    private float currentVisualScaleAbs = 1f;

    // Start: cache components, prepare visual anchor and initial scales -------
    void Start()
    {
        // Cache physics and collider components used by the script.
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();

        // If visualRoot wasn't manually assigned, try to find a child named "Visual".
        if (visualRoot == null)
        {
            Transform found = transform.Find("Visual");
            if (found != null) visualRoot = found;
        }

        // Prefer Animator/SpriteRenderer on the visual child if present,
        // otherwise fall back to components on the root GameObject.
        if (visualRoot != null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
            knightimation = visualRoot.GetComponent<Animator>();
            visualOriginalScale = visualRoot.localScale; // store the starting localScale
        }
        else
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            knightimation = GetComponent<Animator>();
            visualOriginalScale = transform.localScale;
        }

        // Determine initial facing from the original scale's sign,
        // then normalize visualOriginalScale.x to an absolute value to simplify math.
        visualFacing = visualOriginalScale.x >= 0 ? 1 : -1;
        visualOriginalScale.x = Mathf.Abs(visualOriginalScale.x);

        // Create or reuse a persistent anchor positioned at the collider center.
        // This anchor is used as the parent for visualRoot so flips only require
        // changing localScale on the visual and do not move the sprite around.
        if (mainCollider != null && visualRoot != null)
        {
            // Reuse an existing "VisualAnchor" if present (useful during iteration).
            Transform existingAnchor = transform.Find("VisualAnchor");
            if (existingAnchor != null)
            {
                visualAnchor = existingAnchor;
            }
            else
            {
                // Create a new empty GameObject to serve as anchor.
                GameObject anchorGO = new GameObject("VisualAnchor");
                anchorGO.transform.SetParent(transform, false);

                // Convert collider center (world) to local space and set anchor position there.
                Vector3 anchorWorld = mainCollider.bounds.center;
                Vector3 anchorLocal = transform.InverseTransformPoint(anchorWorld);
                anchorGO.transform.localPosition = anchorLocal;
                anchorGO.transform.localRotation = Quaternion.identity;
                anchorGO.transform.localScale = Vector3.one;
                visualAnchor = anchorGO.transform;
            }

            // Reparent visualRoot under the anchor without changing its world position.
            // Using SetParent(visualAnchor, true) preserves world transform, avoiding a jump.
            visualRoot.SetParent(visualAnchor, true);
        }

        // Initialize the absolute visual scale tracker and apply initial scale.
        currentVisualScaleAbs = visualOriginalScale.x * (visualFacing == 1 ? scaleWhenFacingRight : scaleWhenFacingLeft);
        if (visualRoot != null)
            visualRoot.localScale = new Vector3(currentVisualScaleAbs * visualFacing, visualOriginalScale.y, visualOriginalScale.z);
    }

    // Update: read input, handle animation flags, queue jumps -----------------
    void Update()
    {
        // Read raw horizontal and vertical inputs (classic Unity axes)
        float horizontalInput = Input.GetAxis("Horizontal");

        // Move the player with Translate for simple side-scroller movement.
        // NOTE: Using Translate updates transform directly; physics (Rigidbody2D)
        // is used only for jumps and velocity queries here. This keeps horizontal
        // movement simple and deterministic. If you need physics-based horizontal
        // movement (collisions affecting movement), consider using rb.velocity.
        transform.Translate(new Vector3(horizontalInput, 0, 0) * moveSpeed * Time.deltaTime);

        // Animation + visual flipping
        if (horizontalInput > 0)
        {
            if (knightimation != null) knightimation.SetBool("isRunning", true);
            FlipVisual(true); // face right
        }
        else if (horizontalInput < 0)
        {
            if (knightimation != null) knightimation.SetBool("isRunning", true);
            FlipVisual(false); // face left
        }
        else
        {
            if (knightimation != null) knightimation.SetBool("isRunning", false);
        }

        // Immediate overlap checks to determine whether player is touching ground/wall.
        // These checks are done here for responsive input handling (e.g., jumping).
        bool groundNow = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, surfaceCheckRadius, groundLayer) != null;
        bool leftNow = wallCheck1 != null && Physics2D.OverlapCircle(wallCheck1.position, surfaceCheckRadius, groundLayer) != null;
        bool rightNow = wallCheck2 != null && Physics2D.OverlapCircle(wallCheck2.position, surfaceCheckRadius, groundLayer) != null;

        // Jump input handling: prefer normal jump if grounded; otherwise attempt wall-jump
        if (Input.GetButtonDown("Jump"))
        {
            if (groundNow)
            {
                // Queue a grounded jump to be applied in FixedUpdate (physics step).
                shouldJump = true;
                if (knightimation != null) knightimation.SetBool("Jumping", true);
            }
            else if (leftNow || rightNow)
            {
                // Queue a wall-jump and record which direction to push horizontally.
                shouldWallJump = true;
                wallJumpDirection = leftNow ? 1 : -1; // push away from the wall
            }
        }
    }

    // FixedUpdate: apply physics impulses and update animator vertical flags ----
    void FixedUpdate()
    {
        // Update stored contact states (used by other logic and inspector).
        isGrounded = (groundCheck != null) && (Physics2D.OverlapCircle(groundCheck.position, surfaceCheckRadius, groundLayer) != null);
        touchingWallLeft = (wallCheck1 != null) && (Physics2D.OverlapCircle(wallCheck1.position, surfaceCheckRadius, groundLayer) != null);
        touchingWallRight = (wallCheck2 != null) && (Physics2D.OverlapCircle(wallCheck2.position, surfaceCheckRadius, groundLayer) != null);

        // Apply queued grounded jump as an impulse. Clearing vertical velocity first
        // produces a consistent jump height regardless of prior vertical motion.
        if (shouldJump)
        {
            shouldJump = false;
            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Apply queued wall-jump: clear vertical velocity, then apply combined vertical
        // and horizontal impulse away from the wall.
        if (shouldWallJump)
        {
            shouldWallJump = false;

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            Vector2 impulse = Vector2.up * jumpForce + Vector2.right * (wallJumpDirection * wallJumpHorizontalForce);
            rb.AddForce(impulse, ForceMode2D.Impulse);
        }

        // Update animator rising/falling parameters using current vertical velocity.
        // The small threshold avoids flicker when velocity is near zero.
        if (knightimation != null)
        {
            float verticalVel = rb.linearVelocity.y;
            knightimation.SetBool("IsRising", verticalVel > verticalThreshold);
            knightimation.SetBool("IsFalling", verticalVel < -verticalThreshold);
        }

        // When grounded, ensure grounded/jumping animation flags are consistent and clear
        // rising/falling flags to avoid transition edge cases.
        if (isGrounded)
        {
            if (knightimation != null) knightimation.SetBool("Grounded", true);
            if (knightimation != null) knightimation.SetBool("Jumping", false);

            if (knightimation != null)
            {
                knightimation.SetBool("IsRising", false);
                knightimation.SetBool("IsFalling", false);
            }
        }
        else if (knightimation != null)
            knightimation.SetBool("Grounded", false);
    }

    // FlipVisual: change facing of the visual without moving it relative to the collider.
    // This implementation uses a persistent anchor (visualAnchor) placed at the collider center.
    // Reparenting visualRoot under the anchor means flipping can be done by adjusting localScale only.
    void FlipVisual(bool faceRight)
    {
        int desired = faceRight ? 1 : -1;

        if (visualRoot != null)
        {
            // Choose the multiplier for absolute X scale depending on facing.
            float multiplier = faceRight ? scaleWhenFacingRight : scaleWhenFacingLeft;
            float newAbs = visualOriginalScale.x * multiplier;

            // Apply the new localScale on visualRoot. Because visualRoot is parented under
            // visualAnchor (which sits at the collider center), changing localScale flips the visual
            // around that anchor without requiring position mirroring that can accumulate error.
            visualRoot.localScale = new Vector3(newAbs * desired, visualOriginalScale.y, visualOriginalScale.z);

            // Keep track of what we're currently using (useful if you later change multipliers at runtime).
            currentVisualScaleAbs = newAbs;
            visualFacing = desired;
        }
        else if (spriteRenderer != null)
        {
            // Fallback: if no visualRoot exists, flip the sprite using SpriteRenderer.flipX.
            // This method cannot change pivot, so it may not perfectly pivot around the collider center.
            spriteRenderer.flipX = !faceRight;
        }
    }

    // Visual helpers for editor: draw ground/wall check gizmos when object is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, surfaceCheckRadius);

        Gizmos.color = Color.cyan;
        if (wallCheck1 != null)
            Gizmos.DrawWireSphere(wallCheck1.position, surfaceCheckRadius);
        if (wallCheck2 != null)
            Gizmos.DrawWireSphere(wallCheck2.position, surfaceCheckRadius);
    }
}
