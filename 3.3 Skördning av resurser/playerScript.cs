using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;

public class playerScript : MonoBehaviour
{
    public InventoryObject inventory;

    public int playerId;

    public float maxHealth = 100f;
    public float health = 100f;

    [SerializeField] private CharacterController controller;
    [SerializeField] private Camera playerCamera;
    private float horizontalMove;
    private float verticalMove;
    private const float gravityConstant = -9.81f; //cant serialize const's
    private Vector3 playerVelocity;
    private bool canJump = false;
    private bool useCameraLook = true;



    public float movementSpeed = 5f;
    public float jumpHeight = 2f;

    [SerializeField] public float waterLevel = -35.4f;
    bool inWater;
    bool swim;
    public float swimMultiplier = 1.3f;

    PlayerNetwork pN;

    #region - MouseLookScript Variables -
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;
    [SerializeField] private Transform cameraTransform;
    #endregion

    

    private void Awake()
    {
        pN = gameObject.GetComponent<PlayerNetwork>();
        Cursor.lockState = CursorLockMode.Locked;
        playerId = checked((int)gameObject.GetComponent<NetworkIdentity>().netId);
        pN.AskSeerverToCreateInventory(playerId);
    }
    // Update is called once per frame
    void Update()
    {
        if (playerId == 0)
        {
            playerId = checked((int)gameObject.GetComponent<NetworkIdentity>().netId);
            pN.AskSeerverToCreateInventory(playerId);
        }

        horizontalMove = Input.GetAxis("Horizontal")  * movementSpeed;
        verticalMove = Input.GetAxis("Vertical")  * movementSpeed;
        
        #region - TabCode -
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (useCameraLook)
            {
                useCameraLook = !useCameraLook;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                useCameraLook = !useCameraLook;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        #endregion

        #region -Really Ugly Water Code -
        if (this.transform.position.y < waterLevel)
        {
            inWater = true;
        }
        else
        {
            inWater = false;
        }

        if (inWater)
        {
            canJump = false;
        }
        #endregion

        if (controller.isGrounded)
        {
            playerVelocity.y = 0;
            canJump = true;
        }
        else
        {
            playerVelocity.y += gravityConstant * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Space) && canJump)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityConstant);
            canJump = false;
        }
        #region - Swim -
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            swim = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            swim = false;
        }
        if (swim && inWater)
        {
            playerVelocity.y -= gravityConstant * Time.deltaTime;

            if (playerVelocity.y < 5f)
                playerVelocity.y = 5;
            else if (playerVelocity.y >= 8f)
                playerVelocity.y = 8f;
            else
                playerVelocity.y -= gravityConstant * swimMultiplier * Time.deltaTime;
        }
        #endregion
        if (useCameraLook)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Debug.Log("You Clicked");
                RaycastHit hit;
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.distance < 5f)
                    {
                        Transform objectHit = hit.transform;
                        Debug.Log($"Aka {objectHit}");
                        if (objectHit.tag == "Tree")
                        {
                            int treeId = objectHit.GetComponent<IdScript>().id;
                            Debug.Log($"Tree->{treeId}");

                            this.gameObject.GetComponent<NetworkObjectDestroyer>().TellServerToDestroyTree(treeId);
                        }
                        else if (objectHit.tag == "Rock")
                        {
                            int rockId = objectHit.GetComponent<IdScript>().id;
                            Debug.Log($"Rock->{rockId}");

                            this.gameObject.GetComponent<NetworkObjectDestroyer>().TellServerToDestroyRock(rockId);
                        }
                        else if (objectHit.tag == "Enemy")
                        {
                            pN.Kms(objectHit.gameObject);
                        }
                    }

                }
            }
        }

    }

    public void ChangeMouseSensitivity(float _newMouseSensitivity)
    {
        mouseSensitivity = _newMouseSensitivity;
    }

   
    void OnTriggerEnter(Collider collider)
    {
        var item = collider.gameObject.GetComponent<GroundItem>();
        if (item)
        {
            pN.AskServerToAddItem(item.item.Id, 1, playerId);
            pN.Kms(item.gameObject); //hämtar playernetworken och förstör objektet OM och endast om objektet är spawnat på servern
        }
        if(collider.tag == "Enemy")
        {
            TakeDamage(20f);
            Debug.Log(health);
        }
    }

    public void CraftCampfire()
    {
        pN.TellServerToCraftCampFire(playerId);
    }

    private void TakeDamage(float _damage)
    {
        health -= _damage;
        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        pN.Kms(gameObject);
    }

    private void FixedUpdate()
    {
        Vector3 vecForward = playerCamera.transform.forward * verticalMove * Time.deltaTime;
        vecForward = new Vector3(vecForward.x, 0f, vecForward.z);

        Vector3 vecRight = playerCamera.transform.right * horizontalMove * Time.deltaTime;
        vecRight = new Vector3(vecRight.x, 0f, vecRight.z);



        #region - Camera Look -
        if (useCameraLook) { 
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
        #endregion

        controller.Move(vecRight + vecForward + playerVelocity * Time.deltaTime);

    }
    /*
    private void OnApplicationQuit()
    {
        inventory.Container.Clear();
    }*/

    #region - Commands and Server Commands for Destruction of Trees -
    //lol
    #endregion
}
