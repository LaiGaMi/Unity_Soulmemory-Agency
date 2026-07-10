using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;

public class Player_Controller : MonoBehaviour
{
	[Header("角色判定")]
    public int characterID = 1;
	
	[Header("重力設定")]
    public float gravity = -251f;
    public float maxFallSpeed = -50f;
	
	private float verticalVelocity = 0f;
	
	[Header("地面判定")]
	public Transform groundCheck;
	public float groundRadius = 0.475f;
	public LayerMask groundLayer;
	
	private bool isGrounded;
	private float normalgroundRadius;
	
	[Header("攝影機設定")]
    public Transform cameraTransform;
	public Transform cameraLookObject;
	public CinemachineVirtualCamera virtualCamera;
	public float moveDuration = 0.5f;
	public float defaultZ = -7f; //預設攝影機距離
	public float defaultNearZ = -5f; //近的攝影機距離
	public float defaultFarZ = -10f; //遠的攝影機距離
	
	private CinemachineTransposer transposer;
    private Vector3 currentOffset;
    private Coroutine moveCoroutine;
    private float targetZ = -7f;  

	private float cameraLookY;
	
	[Header("Input System")]
    public InputActionReference moveInput;
    public InputActionReference abilityInput;
	
	[Header("Mobile Joystick")]
    public VariableJoystick joystick;
	
	[Header("移動設定")]
    public float moveSpeed = 6f;
	
	private Vector2 moveInputValue;
    private Vector3 moveDirection;
	
	private float normalMoveSpeed;

    private float currentMoveSpeed = 0f;
    private const float speedChangeTime = 0.2f;
	
	[Header("互動設定")]
	public Transform touchCheck;
	public float touchRadius = 0.5f;

    [Header("跳躍設定")]
    public float jumpForce = 11f;
	
	[Header("攀爬設定")]
	public float climbSpeed = 5f;
	public float climbRotateTime = 0.2f; //攀爬轉向時間
	public LayerMask climbLayer;
	
	private float climbTargetZ;
	private bool isClimbing = false;
	private bool isClimbRotating = false;
	
	[Header("潛行設定")]
	public float stealthSpeed = 3f;
	public float stealthSize = 0.5f;
	public Transform headCheck;
	public float headRadius = 0.5f;
	public LayerMask stealthBlockLayer;
	
	private bool isStealth = false; //潛行判定
	
	[Header("推動設定")]
	public LayerMask pushableLayer;
	public float pushPower = 5f;
	

    private CharacterController characterController;
	
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
		transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
		currentOffset = transposer.m_FollowOffset;defaultZ = currentOffset.z;
        targetZ = defaultZ;
		
		normalMoveSpeed = moveSpeed;
		normalgroundRadius = groundRadius;
		cameraLookY = cameraLookObject.localPosition.y;
    }



    private void OnEnable()
    {
        if (moveInput != null)
        {
            moveInput.action.Enable();
        }

        if (abilityInput != null)
        {
            abilityInput.action.Enable();
        }
    }



    private void OnDisable()
    {
        if (moveInput != null)
        {
            moveInput.action.Disable();
        }

        if (abilityInput != null)
        {
            abilityInput.action.Disable();
        }
    }



    void Update()
    {
		GetInput();
		CheckAbilityInput();
		if (!isClimbing)
		{
    		Move();
    		Gravity();
		}
		else
		{
		    if(!isClimbRotating)
    		{
    		    ClimbMove();
    		}
		}
		
		float currentZ = transposer.m_FollowOffset.z;
		if (!Mathf.Approximately(currentZ, targetZ))
        {
            if (moveCoroutine == null)
            {
                moveCoroutine = StartCoroutine(ChangeZ(targetZ));
            }
        }
    }
	
	//移動按鍵
    private void GetInput()
    {
        //PC
        if (moveInput != null)
        {
            moveInputValue = moveInput.action.ReadValue<Vector2>();
        }

        //手機
        if (joystick != null && joystick.Direction.magnitude > 0.1f)
        {
            moveInputValue = joystick.Direction;
        }
    }
	
    //移動
    void Move()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("尚未指定攝影機");
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection =
            cameraForward * moveInputValue.y +
            cameraRight * moveInputValue.x;

        if (moveDirection.magnitude > 0.01f)
        {
            currentMoveSpeed = Mathf.MoveTowards(
                currentMoveSpeed,
                moveSpeed,
                (normalMoveSpeed / speedChangeTime) * Time.deltaTime
            );
            transform.forward = moveDirection.normalized;

            characterController.Move(
                moveDirection.normalized *
                currentMoveSpeed *
                Time.deltaTime
            );
        }
        else
        {
            currentMoveSpeed = Mathf.MoveTowards(
                currentMoveSpeed,
                0f,
                (normalMoveSpeed / speedChangeTime) * Time.deltaTime
            );
			
            if(currentMoveSpeed > 0)
            {
                characterController.Move(
                    transform.forward *
                    currentMoveSpeed *
                    Time.deltaTime
                );
            }
        }
    }
	
	//重力
    void Gravity()
    {
		//腳底球形判定
    	isGrounded = Physics.CheckSphere(
    	    groundCheck.position,
    	    groundRadius,
    	    groundLayer
    	);
		
        if(isGrounded)
        {
			if(verticalVelocity < 0)
			{
				verticalVelocity = -0.1f;
			}
        }
        else
        {
            //重力持續增加
            verticalVelocity += gravity * Time.deltaTime;

            //限制最大下墜速度
            if(verticalVelocity < maxFallSpeed)
            {
                verticalVelocity = maxFallSpeed;
            }
        }

        characterController.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
    }
	
	//能力按鍵
    private void CheckAbilityInput()
    {
        if (abilityInput != null)
        {
            if (abilityInput.action.WasPressedThisFrame())
            {
                AbilityInput();
            }
        }
    }
	
	//根據角色決定操作
    public void AbilityInput()
    {
        switch(characterID)
        {
            //主角 跳躍
            case 1:
                Jump();
                break;

            //刑警 攀爬
            case 2:

                Climb();
                break;

            //狐狸 潛行
            case 3:

                Stealth();
                break;

            //狗狗 推動
            case 4:

                Push();
                break;
        }
    }
	
	//跳躍
    void Jump()
    {
        if(isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }
	
	//攀爬移動
	void ClimbMove()
    {
		//檢查是否還接觸攀爬物
        if(!CheckClimbObject())
        {
            isClimbing = false;
            verticalVelocity = 0;
			targetZ = defaultZ;
            return;
        }
		
        Vector3 climbDirection = Vector3.zero;

        // 前後輸入改為上下移動
        climbDirection =
            Vector3.up *
            moveInputValue.y;

        // 左右移動保留
        climbDirection +=
            transform.right *
            moveInputValue.x;

        if(climbDirection.magnitude > 0.01f)
        {
            transform.forward = transform.forward;

            characterController.Move(
                climbDirection.normalized *
                climbSpeed *
                Time.deltaTime
            );
        }
    }
	
	//攀爬判定
	bool CheckClimbObject()
	{
	    Collider[] climbObjects = Physics.OverlapSphere(
	        touchCheck.position,
	        touchRadius,
	        climbLayer
	    );

	    if(climbObjects.Length > 0)
	    {
	        return true;
	    }

	    return false;
	}
	
	//攀爬
	void Climb()
	{
		//如果已經在攀爬，直接退出
    	if(isClimbing)
    	{
    	    isClimbing = false;
			targetZ = defaultZ;
    	    return;
    	}


        //檢查前方是否有攀爬物
    	Collider[] climbObjects = Physics.OverlapSphere(
    	    touchCheck.position,
    	    touchRadius,
    	    climbLayer
    	);

    	//沒有攀爬物，不進入攀爬
    	if(climbObjects.Length == 0)
    	{
    	    return;
    	}

    	//取得攀爬物件Z軸位置
    	climbTargetZ = climbObjects[0].transform.position.z;

    	//進入攀爬
    	isClimbing = true;

    	//停止下墜
    	verticalVelocity = 0;

    	StartCoroutine(ClimbRotate());
		
		targetZ = defaultFarZ;
		}
	
	IEnumerator ClimbRotate()
	{
	    isClimbRotating = true;

	    //禁止目前移動
	    currentMoveSpeed = 0;

	    //取得攝影機水平方向
	    Vector3 cameraForward = cameraTransform.forward;
	    cameraForward.y = 0;
	    cameraForward.Normalize();

	    //目標旋轉
	    Quaternion targetRotation =
	        Quaternion.LookRotation(cameraForward);

	    Quaternion startRotation = transform.rotation;

	    Vector3 startPosition = transform.position;

	    //設定目標z位置
	    Vector3 targetPosition = new Vector3(
	        transform.position.x,
	        transform.position.y,
	        climbTargetZ
	    );

	    float timer = 0;

	    while(timer < climbRotateTime)
	    {
	        timer += Time.deltaTime;
	        float t = timer / climbRotateTime;

	        //旋轉
	        transform.rotation =
	            Quaternion.Lerp(
	                startRotation,
	                targetRotation,
	                t
	            );
	        //移動
	        transform.position =
	            Vector3.Lerp(
	                startPosition,
	                targetPosition,
	                t
	            );
	        yield return null;
	    }

	    //確保角度位置正確
	    transform.rotation = targetRotation;
	    transform.position = targetPosition;

	    isClimbRotating = false;
	}
	
	//潛行
	void Stealth()
	{
		Vector3 pos = cameraLookObject.localPosition;
		
   	 	if(!isStealth)
	    {
	        isStealth = true;
			
	        float oldHeight = characterController.height;

            transform.localScale = new Vector3(
                transform.localScale.x,
                stealthSize,
                transform.localScale.z
            );
			
            characterController.height = oldHeight;
            characterController.center = new Vector3(
                characterController.center.x,
                characterController.height / 2f,
                characterController.center.z
            );

            Vector3 posP = transform.position;
            posP.y -= (oldHeight - characterController.height) / 2f;
            transform.position = new Vector3(posP.x, posP.y, posP.z);
			
	        moveSpeed = stealthSpeed;
			groundRadius = normalgroundRadius / 1.6f;
			
        	pos.y = cameraLookY / stealthSize;
			
			targetZ = defaultNearZ;
	    }
	    else
	    {
	        if(CheckHeadObject())
	        {
	        	return;
	        }

            isStealth = false;
	
	        float oldHeight = characterController.height;

            transform.localScale = new Vector3(
                transform.localScale.x,
                1f,
                transform.localScale.z
            );

            characterController.height = 2;
            characterController.center = new Vector3(
                characterController.center.x,
                characterController.height / 2f,
                characterController.center.z
            );

            Vector3 posP = transform.position;
            posP.y += (characterController.height - oldHeight) / 2f;
            transform.position = new Vector3(posP.x, posP.y, posP.z);
			
	        moveSpeed = normalMoveSpeed;
			groundRadius = normalgroundRadius;
			
        	pos.y = cameraLookY;
			
			targetZ = defaultZ;
	    }
		
		cameraLookObject.localPosition = pos;
	}
	
	//潛行解除頭頂判定
	bool CheckHeadObject()
	{
		Collider[] headObjects = Physics.OverlapSphere(
			headCheck.position,
			headRadius,
			stealthBlockLayer
		);


		if(headObjects.Length > 0)
		{
			return true;
		}


		return false;
	}
	
	//推動
	void Push()
	{
		//檢查前方是否有可推物
		Collider[] pushObjects = Physics.OverlapSphere(
			touchCheck.position,
			touchRadius,
			pushableLayer
		);

		//沒有可推物
		if(pushObjects.Length == 0)
		{
			return;
		}

		//取得第一個可推物
		CanPush pushObject =
			pushObjects[0].GetComponent<CanPush>();
			
		//確認是否可以推
		if(pushObject != null)
		{
			Vector3 pushDirection =
				transform.forward;

			pushObject.PushObject(
				pushDirection,
				pushPower
			);
		}
	}
	
	
	//攝影機縮放
	private System.Collections.IEnumerator ChangeZ(float target)
    {
        Vector3 startOffset = transposer.m_FollowOffset;

        Vector3 endOffset = startOffset;
        endOffset.z = target;


        float timer = 0f;


        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = timer / moveDuration;

            // 曲線平滑
            t = Mathf.SmoothStep(0f, 1f, t);

            transposer.m_FollowOffset =
                Vector3.Lerp(startOffset, endOffset, t);

            yield return null;
        }


        transposer.m_FollowOffset = endOffset;

        moveCoroutine = null;
    }
	
	
	//腳底判定顯示
	private void OnDrawGizmosSelected()
	{
    	if(groundCheck == null)
    	    return;
	
	    Gizmos.color = Color.red;
	
   	    Gizmos.DrawWireSphere(
   	        groundCheck.position,
   	        groundRadius
   	    );
	    if(touchCheck != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                touchCheck.position,
                touchRadius
            );
        }
		if(headCheck != null)
	    {
	        Gizmos.color = Color.green;

	        Gizmos.DrawWireSphere(
	            headCheck.position,
	            headRadius
	        );
	    }
	}
}
