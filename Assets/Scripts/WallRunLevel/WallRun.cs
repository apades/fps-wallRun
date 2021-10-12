using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

///<summary></summary>
[RequireComponent(typeof(PlayerCharacterController))]
public class WallRun : MonoBehaviour {

    ///<summary>滑墙检测触发距离</summary>
    public float wallMaxDistance = 1;
    ///<summary>滑墙速度乘积</summary>
    public float wallSpeedMultiplier = 1.2f;
    public float minimumHeight = 1.2f;
    public float maxAngleRoll = 20;
    [Range(0.0f, 1.0f)]
    public float normalizedAngleThreshold = 0.1f;

    public float jumpDuration = 1;
    public float wallBouncing = 3;
    public float cameraTransitionDuration = 1;

    public float wallGravityDownForce = 20f;

    public bool useSprint;


    [Space]
    public Volume wallRunVolume;

    PlayerCharacterController m_PlayerCharacterController;
    PlayerInputHandler m_InputHandler;

    ///<summary>检测是否靠着墙壁的5个方向</summary>
    Vector3[] directions = new Vector3[]{
        Vector3.right,
        Vector3.right + Vector3.forward,
        Vector3.forward,
        Vector3.left + Vector3.forward,
        Vector3.left
    };
    ///<summary>directions命中的墙壁</summary>
    RaycastHit[] hits;

    public bool isWallRunning = false;
    Vector3 lastWallPosition;
    Vector3 lastWallNormal;
    float elapsedTimeSinceJump = 0;
    ///<summary>进入滑墙时长</summary>
    float elapsedTimeSinceWallAttach = 0;
    ///<summary>距离退出滑墙时长</summary>
    float elapsedTimeSinceWallDetatch = 0;
    bool jumping;
    float lastVolumeValue = 0;
    float noiseAmplitude;

    bool isPlayerGrounded() => m_PlayerCharacterController.isGrounded;

    public bool IsWallRunning() => isWallRunning;

    bool CanWallRun() {
        float verticalAxis = Input.GetAxisRaw(GameConstants.k_AxisNameVertical);
        bool isSprinting = m_InputHandler.GetSprintInputHeld();
        isSprinting = !useSprint ? true : isSprinting;

        // 不在地面，前进速度>0，脚下无可碰撞单位，奔跑状态
        return !isPlayerGrounded() && verticalAxis > 0 && VerticalCheck() && isSprinting;
    }

    ///<summary>检测脚下是否有可碰撞单位</summary>
    bool VerticalCheck() {
        return !Physics.Raycast(transform.position, Vector3.down, minimumHeight);
    }


    void Start() {
        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        m_InputHandler = GetComponent<PlayerInputHandler>();


        if (wallRunVolume != null) {
            SetVolumeWeight(0);
        }
    }


    public void LateUpdate() {
        isWallRunning = false;

        if (m_InputHandler.GetJumpInputDown())
            jumping = true;

        // 非跳跃状态才能继续滑墙
        if (CanAttach()) {
            hits = new RaycastHit[directions.Length];

            for (int i = 0; i < directions.Length; i++) {
                Vector3 dir = transform.TransformDirection(directions[i]);
                Physics.Raycast(transform.position, dir, out hits[i], wallMaxDistance);
                // debug line，绿色表示hit墙壁，红色未空
                if (hits[i].collider != null) {
                    Debug.DrawRay(transform.position, dir * hits[i].distance, Color.green);
                } else {
                    Debug.DrawRay(transform.position, dir * wallMaxDistance, Color.red);
                }
            }

            if (CanWallRun()) {
                hits = hits.ToList().Where(h => h.collider != null).OrderBy(h => h.distance).ToArray();
                if (hits.Length > 0) {
                    var hit = hits[0];
                    OnWall(hit);
                    lastWallPosition = hit.point;
                    lastWallNormal = hit.normal;
                }
            }
        }

        if (isWallRunning) {
            elapsedTimeSinceWallDetatch = 0;
            if (elapsedTimeSinceWallAttach == 0 && wallRunVolume != null) {
                lastVolumeValue = wallRunVolume.weight;
            }
            elapsedTimeSinceWallAttach += Time.deltaTime;
            m_PlayerCharacterController.characterVelocity += Vector3.down * wallGravityDownForce * Time.deltaTime;
        } else {
            elapsedTimeSinceWallAttach = 0;
            if (elapsedTimeSinceWallDetatch == 0 && wallRunVolume != null) {
                lastVolumeValue = wallRunVolume.weight;
            }
            elapsedTimeSinceWallDetatch += Time.deltaTime;
        }

        if (wallRunVolume != null) {
            HandleVolume();
        }
    }

    bool CanAttach() {
        if (jumping) {
            elapsedTimeSinceJump += Time.deltaTime;
            if (elapsedTimeSinceJump > jumpDuration) {
                elapsedTimeSinceJump = 0;
                jumping = false;
            }
            return false;
        }

        return true;
    }

    void OnWall(RaycastHit hit) {
        /*
        hit.normal是命中位置的切线 https://answers.unity.com/questions/478017/what-is-a-raycasthit-normal.html
        Vector3.Dot是计算点乘积 https://blog.csdn.net/weixin_44446603/article/details/115913015，输出的值为-1~1，x>0为锐角， =0为直角，<0为钝角
        */
        float d = Vector3.Dot(hit.normal, Vector3.up);
        // ? 为什么要判断偏差值，去掉也能用
        // (-0.1 <= x <= 0.1)
        if (d >= -normalizedAngleThreshold && d <= normalizedAngleThreshold) {
            // Vector3 alongWall = Vector3.Cross(hit.normal, Vector3.up);
            float vertical = Input.GetAxisRaw(GameConstants.k_AxisNameVertical);
            Vector3 alongWall = transform.TransformDirection(Vector3.forward);

            // 角色滑行方向
            Debug.DrawRay(transform.position, alongWall.normalized * 10, Color.green);
            // 滑行墙体的切线
            Debug.DrawRay(transform.position, lastWallNormal * 10, Color.magenta);

            m_PlayerCharacterController.characterVelocity = alongWall * vertical * wallSpeedMultiplier;
            isWallRunning = true;
        }
    }

    float CalculateSide() {
        if (isWallRunning) {
            Vector3 heading = lastWallPosition - transform.position;
            Vector3 perp = Vector3.Cross(transform.forward, heading);
            float dir = Vector3.Dot(perp, transform.up);
            return dir;
        }
        return 0;
    }

    ///<summary>滑墙状态的镜头z偏移</summary>
    public float GetCameraRoll() {
        float dir = CalculateSide();
        float cameraAngle = m_PlayerCharacterController.playerCamera.transform.eulerAngles.z;
        float targetAngle = 0;
        if (dir != 0) {
            targetAngle = Mathf.Sign(dir) * maxAngleRoll;
        }
        return Mathf.LerpAngle(cameraAngle, targetAngle, Mathf.Max(elapsedTimeSinceWallAttach, elapsedTimeSinceWallDetatch) / cameraTransitionDuration);
    }

    public Vector3 GetWallJumpDirection() {
        if (isWallRunning) {
            return lastWallNormal * wallBouncing + Vector3.up;
        }
        return Vector3.zero;
    }

    ///<summary>处理hdpr镜头效果</summary>
    void HandleVolume() {
        float w = 0;
        if (isWallRunning) {
            w = Mathf.Lerp(lastVolumeValue, 1, elapsedTimeSinceWallAttach / cameraTransitionDuration);
        } else {
            w = Mathf.Lerp(lastVolumeValue, 0, elapsedTimeSinceWallDetatch / cameraTransitionDuration);
        }

        SetVolumeWeight(w);
    }

    void SetVolumeWeight(float weight) {
        wallRunVolume.weight = weight;
    }
}
