using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class NetworkManager : MonoBehaviourPunCallbacks
{
    private bool RandEventCheck = false; // 랜덤이벤트 설정 체크용 
    public NetworkPlayerMove NPM;

    [Header("DisconnectPanel")]
    public InputField NickNameInput;
    public GameObject DisconnectPanel;

    [Header("LobbyPanel")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;

    [Header("RoomPanel")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button StartButton;

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;
    public GameObject playerPrefab; // 네트워크 플레이어 프리팹
    public GameObject[] maps; // 사용할 맵 프리팹 배열

    [Header("Character Selection")]
    public Button[] characterButtons; // 캐릭터 선택 버튼들
    public GameObject[] characterPrefabs; // 각 캐릭터의 프리팹들

    [Header("Game Finish")]
    public GameObject GameFinishPanel; // 게임 종료 패널 참조
    public Text CountdownText; // 카운트다운 텍스트 참조
    public Button ContinueButton; // 계속하기 버튼
    public Text ContinueButtonText; // 계속하기 버튼 텍스트

    public Text TimeText;
    private List<RoomInfo> myList = new List<RoomInfo>();
    private int currentPage = 1, maxPage, multiple;
    private Coroutine countdownCoroutine; // 카운트다운 코루틴 제어
    private Coroutine gameTimerCoroutine; // 게임 타이머 코루틴 제어
    private GameObject currentMap; // 현재 맵 인스턴스 저장 변수
    private float elapsedTime = 0f; // 경과 시간을 추적할 변수
    private bool gameRunning = true; // 게임이 진행 중인지 여부를 추적할 변수
    private bool countdownStarted = false; // 카운트다운이 시작되었는지 여부를 추적하는 변수
    private bool continueButtonClicked = false; // 계속하기 버튼이 클릭되었는지 여부

    public AudioClip characterSelectSound; // 캐릭터 선택 사운드
    public AudioClip startButtonSound; // 스타트 버튼 사운드
    public AudioClip restartButtonSound; // 리스타트 버튼 사운드
    public AudioClip continueButtonSound; // 계속하기 버튼 사운드
    private AudioSource audioSource; // 사운드 추가 
    public AudioClip countdownClip; // 카운트 시 사운드

    public Text[] rankingTexts; // 게임 피니쉬 패널에 있는 랭킹 텍스트 배열
    private struct PlayerData
    {
        public string nickName;
        public float elapsedTime;
        public bool hasFinished;
        public int selectedCharacterIndex; // 선택된 캐릭터 인덱스 추가
    }
    private int selectedCharacterIndex = -1; // 선택된 캐릭터가 없으면 -1로 설정
    private List<PlayerData> playerDataList = new List<PlayerData>();
    private Dictionary<string, PlayerData> playerFinishTimes = new Dictionary<string, PlayerData>();

    private int continueButtonPressCount = 0; // 계속하기 버튼을 누른 사람 수

    [Header("Navigation Bar")]
    public GameObject navigationBarImage; // 네비게이션 바 참조
    public GameObject playerIndicatorPrefab; // 본인 플레이어 인디케이터 프리팹

    private Transform finishTransform; // Finish 태그를 가진 오브젝트의 Transform
    private Transform playerTransform; // 플레이어의 Transform

    private Image playerIndicator; // 본인 플레이어 인디케이터
    private float startX; // startX를 클래스 변수로 선언

    [Header("In-Game Chat")]
    public GameObject ChatPanel;
    public InputField InGameChatInput;
    public Text[] InGameChatText;
    private bool isChatInputActive = false; // 채팅 입력 필드 활성화 상태를 추적
    private bool isInGame = false; // 게임 중인지 여부를 추적

    [Header("Event Panels")]
    public GameObject tripleJumpPanel;
    public GameObject doubleJumpPowerPanel;
    public GameObject maxSpeedPanel;

    void Start()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // 루프 변수 캡처
            characterButtons[i].onClick.AddListener(() => OnCharacterButtonClicked(index));
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect(); // 이미 연결된 상태라면 초기화
        }

        ContinueButton.onClick.AddListener(OnContinueButtonClicked); // 계속하기 버튼 클릭 리스너 추가

        // AudioSource 설정
        audioSource = gameObject.AddComponent<AudioSource>();
        countdownClip = Resources.Load<AudioClip>("MultiGameSound/CountdownClip"); // 카운트다운 사운드 클립 로드
        if (countdownClip == null)
        {
            Debug.LogError("CountdownClip not found in Resources/MultiGameSound");
        }

        // 오디오 클립 설정
        characterSelectSound = Resources.Load<AudioClip>("MultiGameSound/CharacterSelectSound");
        startButtonSound = Resources.Load<AudioClip>("MultiGameSound/StartButtonSound");
        restartButtonSound = Resources.Load<AudioClip>("MultiGameSound/RestartButtonSound");
        continueButtonSound = Resources.Load<AudioClip>("MultiGameSound/ContinueButtonSound");

        if (characterSelectSound == null || startButtonSound == null || restartButtonSound == null || continueButtonSound == null)
        {
            Debug.LogError("Sound clips not found in Resources/MultiGameSound");
        }
    
    // 기존 초기화 코드 유지
    // AudioSource 설정
    audioSource = gameObject.AddComponent<AudioSource>();

        // 채팅 입력 필드 리스너 추가
        InGameChatInput.onEndEdit.AddListener(delegate { OnInGameChatInputEndEdit(InGameChatInput); });

        // 채팅 패널 비활성화
        ChatPanel.SetActive(false);

        // 이벤트 패널 비활성화
        tripleJumpPanel.SetActive(false);
        doubleJumpPowerPanel.SetActive(false);
        maxSpeedPanel.SetActive(false);
    }

    public bool IsChatInputActive()
    {
        return isChatInputActive;
    }

    public void OnCharacterButtonClicked(int index)
    {
        selectedCharacterIndex = index;
        Debug.Log("선택된 캐릭터 인덱스: " + index);

        // 캐릭터 선택 사운드 재생
        if (audioSource != null && characterSelectSound != null)
        {
            audioSource.PlayOneShot(characterSelectSound, 0.1f);
        }
    }

    private void FindFinishObject()
    {
        GameObject finishObject = GameObject.FindGameObjectWithTag("Finish");
        if (finishObject != null)
        {
            finishTransform = finishObject.transform;
            Debug.Log($"Finish object found at position: {finishTransform.position}");
        }
        else
        {
            Debug.LogError("Finish object not found!");
        }
    }

    void Update()
    {
        SendByEnter();
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";

        if (PhotonNetwork.InRoom)
        {
            // 진행 상황 업데이트
            if (playerTransform != null && finishTransform != null)
            {
                UpdatePlayerIndicators();
            }
        }

        if (RandEventCheck = false &&elapsedTime == 5f)
        {
            NPM.PlayerEvent();
            RandEventCheck = true;
        }

        // 게임 중일 때만 엔터 키 입력 감지 및 채팅 입력 필드 활성화/비활성화
        if (isInGame && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (isChatInputActive)
            {
                if (!string.IsNullOrWhiteSpace(InGameChatInput.text))
                {
                    SendInGameChat();
                }
                DeactivateChatInput();
            }
            else
            {
                ActivateChatInput();
            }
        }
    }

    private void ActivateChatInput()
    {
        isChatInputActive = true;
        InGameChatInput.gameObject.SetActive(true); // 입력 필드 활성화
        InGameChatInput.Select(); // 입력 필드에 커서 유지
        InGameChatInput.ActivateInputField(); // 입력 필드 활성화
    }

    private void DeactivateChatInput()
    {
        isChatInputActive = false;
        InGameChatInput.gameObject.SetActive(false); // 입력 필드 비활성화
    // 다른 UI 요소로 포커스 옮기기 (필요 시)
    // 예: 다른 버튼이나 패널 선택
    }


    public void SendByEnter()
    {
        // Enter 키를 감지하기 위해 입력 필드에 리스너를 추가합니다.
        ChatInput.onEndEdit.AddListener(delegate { OnChatInputEndEdit(ChatInput); });
    }

    // 사용자가 채팅 입력 필드 편집을 마쳤을 때 호출됩니다.
    void OnChatInputEndEdit(InputField input)
    {
        // 입력 필드가 비어있거나 null인지 확인합니다.
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return;

        // Enter 키가 눌렸는지 확인합니다 (KeyCode.Return).
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            // 메시지를 보내기 위해 Send 메서드를 호출합니다.
            Send();
        }
    }

    public void Connect()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Disconnected || PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.LogWarning("Already connected or connecting.");
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        WelcomeText.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다";
        myList.Clear();
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (LobbyPanel != null)
        {
            LobbyPanel.SetActive(false);
        }

        if (RoomPanel != null)
        {
            RoomPanel.SetActive(false);
        }

        if (DisconnectPanel != null)
        {
            DisconnectPanel.SetActive(true);
        }

        Connect();
    }

    #region 방리스트 갱신
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }

    void MyListRenewal()
    {
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;

        PreviousBtn.interactable = (currentPage > 1);
        NextBtn.interactable = (currentPage < maxPage);

        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            if (multiple + i < myList.Count)
            {
                CellBtn[i].interactable = true;
                CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = myList[multiple + i].Name;
                CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers;
            }
            else
            {
                CellBtn[i].interactable = false;
                CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = "";
                CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = "";
            }
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for (int i = 0; i < roomCount; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                if (!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if (myList.IndexOf(roomList[i]) != -1) myList.RemoveAt(myList.IndexOf(roomList[i]));
        }
        MyListRenewal();
    }
    #endregion

    #region 서버연결
    void Awake()
    {
        Screen.SetResolution(1600, 900, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
    }
    #endregion

    #region 방
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + Random.Range(0, 100) : RoomInput.text, new RoomOptions { MaxPlayers = 8 });

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        RoomPanel.SetActive(true);
        RoomRenewal();
        ChatInput.text = "";
        for (int i = 0; i < ChatText.Length; i++)
        {
            ChatText[i].text = "";
        }
        // StartButton 활성화
        StartButton.interactable = PhotonNetwork.IsMasterClient;

        // Log joined room information for debugging
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Player count: " + PhotonNetwork.CurrentRoom.PlayerCount);

        // 중복된 플레이어 프리팹 삭제
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in existingPlayers)
        {
            Destroy(player);
        }

        // 캐릭터 선택 후 해당 프리팹을 생성하는 로직 추가
        //Vector3 spawnPosition = new Vector3(-25.91f, -9.57f, -1); // 플레이어 스폰 위치
        //GameObject spawnedPlayer = null; // 플레이어 변수 선언
        //if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterPrefabs.Length)
        //{
        //    spawnedPlayer = PhotonNetwork.Instantiate(characterPrefabs[selectedCharacterIndex].name, spawnPosition, Quaternion.identity);
        //}
        //else
        //{
        //    spawnedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        //}

        //playerTransform = spawnedPlayer.transform; // 플레이어 Transform 저장
        //startX = spawnPosition.x; // 플레이어의 초기 위치를 startX로 설정

        // 이미 생성된 플레이어 프리팹을 가져와서 playerTransform을 설정
        var existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            playerTransform = existingPlayer.transform;
            startX = playerTransform.position.x; // 플레이어의 초기 위치를 startX로 설정
        }

        // 게임 피니쉬 패널 초기화
        GameFinishPanel.SetActive(false);
        CountdownText.text = "";
        ContinueButtonText.text = "Continue";
        continueButtonClicked = false;
        continueButtonPressCount = 0;
        ContinueButton.interactable = true;

        // 타이머 초기화
        elapsedTime = 0f;
        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
        }
        TimeText.text = "Time: 0분 : 00.0초";

        // 네비게이션 바 비활성화 (게임 시작 전까지)
        navigationBarImage.SetActive(false);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        RoomInput.text = "";
        CreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomInput.text = "";
        CreateRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다</color>");

        Debug.Log("Player entered room: " + newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다</color>");

        Debug.Log("Player left room: " + otherPlayer.NickName);
    }
    #endregion

    #region 채팅
    public void Send()
    {
        PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName + " : " + ChatInput.text);
        ChatInput.text = "";
        ChatInput.Select(); // 인풋 필드에 커서 유지
        ChatInput.ActivateInputField(); // 인풋 필드 활성화
    }

    [PunRPC] // RPC는 플레이어가 속해있는 방 모든 인원에게 전달한다
    void ChatRPC(string msg)
    {
        bool isInput = false;
        for (int i = 0; i < ChatText.Length; i++)
            if (ChatText[i].text == "")
            {
                isInput = true;
                ChatText[i].text = msg;
                break;
            }
        if (!isInput) // 꽉차면 한칸씩 위로 올림
        {
            for (int i = 1; i < ChatText.Length; i++) ChatText[i - 1].text = ChatText[i].text;
            ChatText[ChatText.Length - 1].text = msg;
        }

        // In-Game Chat Text 업데이트
        isInput = false;
        for (int i = 0; i < InGameChatText.Length; i++)
        {
            if (InGameChatText[i].text == "")
            {
                isInput = true;
                InGameChatText[i].text = msg;
                break;
            }
        }

        if (!isInput)
        {
            for (int i = 1; i < InGameChatText.Length; i++) InGameChatText[i - 1].text = InGameChatText[i].text;
            InGameChatText[InGameChatText.Length - 1].text = msg;
        }
    }
    #endregion

    #region 게임 시작 및 맵 로딩
    public void OnStartButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int randomMapIndex = Random.Range(0, maps.Length);
            int randomSeed = Random.Range(0, 10000); // 랜덤 시드 생성
            PV.RPC("RPC_StartGame", RpcTarget.All, randomMapIndex, randomSeed); 
        }

        // 스타트 버튼 사운드 재생
        if (audioSource != null && startButtonSound != null)
        {
            audioSource.PlayOneShot(startButtonSound, 0.5f);
        }
    }

    private void StartGameTimer()
    {
        // 타이머 초기화
        elapsedTime = 0f;
        TimeText.gameObject.SetActive(true);
        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
        }
        gameTimerCoroutine = StartCoroutine(GameTimerCoroutine());
    }

    private IEnumerator GameTimerCoroutine()
    {
        while (gameRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimeText(elapsedTime);
            UpdatePlayerIndicators(); // 게임 타이머 코루틴에서 인디케이터 업데이트
            yield return null;
        }
    }

    private void UpdateTimeText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        float seconds = time % 60F;
        TimeText.text = string.Format("Time: {0}분 : {1:00.0}초", minutes, seconds);
    }

    public void OnContinueButtonClicked()
    {
        if (continueButtonClicked) return; // 이미 클릭된 경우 무시

        continueButtonClicked = true; // 버튼 클릭 상태 업데이트
        ContinueButton.interactable = false; // 더 이상 클릭할 수 없도록 비활성화

        // 계속하기 버튼 사운드 재생
        if (audioSource != null && continueButtonSound != null)
        {
            audioSource.PlayOneShot(continueButtonSound, 0.2f); // 볼륨을 30%로 줄여서 재생
        }

        // RPC 호출하여 모든 클라이언트에 클릭 수 업데이트
        PV.RPC("RPC_UpdateContinueButtonCount", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName);
    }

    [PunRPC]
    private void RPC_UpdateContinueButtonCount(string playerName)
    {
        continueButtonPressCount++;
        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        ContinueButtonText.text = $"{continueButtonPressCount}/{totalPlayers} Players Ready";

        // 모든 플레이어가 "계속하기" 버튼을 클릭한 경우
        if (continueButtonPressCount == totalPlayers)
        {
            PV.RPC("RPC_StartCountdown", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_StartCountdown()
    {
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        int countdown = 3;

        while (countdown > 0)
        {
            CountdownText.text = countdown.ToString();
            yield return new WaitForSeconds(1);
            countdown--;
        }

        CountdownText.text = "Go!";
        yield return new WaitForSeconds(1);

        RestartGame();
    }

    private void RestartGame()
    {
        int randomMapIndex = Random.Range(0, maps.Length);
        int randomSeed = Random.Range(0, 10000); // 랜덤 시드 생성
        PV.RPC("RPC_RestartGame", RpcTarget.All, randomMapIndex, randomSeed);

        // 리스타트 버튼 사운드 재생
        if (audioSource != null && restartButtonSound != null)
        {
            audioSource.PlayOneShot(restartButtonSound, 0.2f); // 볼륨을 30%로 줄여서 재생
        }
    }

    [PunRPC]
    private void RPC_RestartGame(int mapIndex, int seed)
    {
        // 초기화 로직
        ResetGameVariables();
        PV.RPC("RPC_RemoveExistingPlayers", RpcTarget.All);
        RemoveExistingPlayers();

        // 게임 시작 로직 호출
        StartGame(mapIndex, seed);
        GameFinishPanel.SetActive(false);

         // 네비게이션 바 활성화 및 인디케이터 재생성
        navigationBarImage.SetActive(true);
        InitializeNavigationBar();
    }

    [PunRPC]
    private void RPC_RemoveExistingPlayers()
    {
        RemoveExistingPlayers();
    }

    private void RemoveExistingPlayers()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player.GetComponent<PhotonView>().IsMine)
            {
                PhotonNetwork.Destroy(player);
            }
        }
    }

    private void ResetGameVariables()
    {
        // 타이머 초기화
        elapsedTime = 0f;
        RandEventCheck = false; // 설정했던 랜덤 이벤트 체크 초기화
        TimeText.text = "Time: 0분 : 00.0초";

        // 경과 시간 초기화
        elapsedTime = 0f;

        // 게임 진행 상태 초기화
        gameRunning = true;

        // Coroutine 초기화
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
        }

        // 맵 초기화
        if (currentMap != null)
        {
            Destroy(currentMap);
            currentMap = null;
        }

        // 플레이어 데이터 초기화
        playerFinishTimes.Clear();
        selectedCharacterIndex = -1;

        // "계속하기" 버튼 초기화
        continueButtonClicked = false;
        continueButtonPressCount = 0;
        ContinueButtonText.text = "Continue";
        ContinueButton.interactable = true;

        // 네비게이션 바 초기화
        navigationBarImage.SetActive(false);
        if (playerIndicator != null)
        {
            Destroy(playerIndicator.gameObject);
            playerIndicator = null;
        }

        // 채팅 입력 필드 초기화 및 비활성화
        DeactivateChatInput();
        InGameChatInput.text = "";

        // 게임 종료 시 게임 상태를 false로 설정
        isInGame = false;
    }

    private void StartGame(int mapIndex, int seed)
    {
        // 게임 시작 RPC를 호출하여 모든 플레이어에게 맵을 생성하도록 합니다.
        PV.RPC("RPC_StartGame", RpcTarget.All, mapIndex, seed);
    }

    [PunRPC]
    private void RPC_StartGame(int mapIndex, int seed)
    {
        // Deactivate RoomPanel
        RoomPanel.SetActive(false);
        DisconnectPanel.SetActive(false);
        LobbyPanel.SetActive(false);

        // Set the seed for random number generation
        Random.InitState(seed);

        // Load selected map
        if (currentMap != null)
        {
            Destroy(currentMap);
        }
        currentMap = Instantiate(maps[mapIndex], Vector3.zero, Quaternion.identity);
        FindFinishObject();
        // finishTransform = finishObject.transform;

        // 채팅 패널 활성화
        ChatPanel.SetActive(true);

        // Remove existing player prefabs
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            Destroy(player);
        }

        // Create players
        Vector3 spawnPosition = new Vector3(-25.91f, -9.57f, -1); // Example spawn position
        GameObject spawnedPlayer = null;

        // 플레이어 프리팹을 생성하고 playerTransform을 설정
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterPrefabs.Length)
        {
            spawnedPlayer = PhotonNetwork.Instantiate(characterPrefabs[selectedCharacterIndex].name, spawnPosition, Quaternion.identity);
        }
        else
        {
            spawnedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        }

        playerTransform = spawnedPlayer.transform; // 플레이어 Transform 저장
        startX = spawnPosition.x; // 플레이어의 초기 위치를 startX로 설정

        // Initialize navigation bar and indicators
        InitializeNavigationBar();

        // 게임 시작 시 게임 상태를 true로 설정
        isInGame = true;


        // Start game timer for all clients
        PV.RPC("RPC_StartGameTimer", RpcTarget.All);
    }

    private void UpdatePlayerIndicators()
    {
        if (playerTransform == null || finishTransform == null) return;

        // 네비게이션 바의 RectTransform
        RectTransform navBarRect = navigationBarImage.GetComponent<RectTransform>();

        // 플레이어의 시작 위치와 피니쉬 위치
        float finishX = finishTransform.position.x;

        // 플레이어와 피니쉬 포인트 사이의 총 거리 (x축 기준)
        float totalDistance = Mathf.Abs(finishX - startX);

        if (totalDistance <= 0) return;

        // 현재 플레이어의 진행 상황
        float currentDistance = playerTransform.position.x - startX;

        // 진행 상황 비율 (0에서 1 사이의 값)
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        // 인디케이터의 위치 계산 (왼쪽 끝에서 시작하여 오른쪽으로 진행)
        float indicatorPosition = -navBarRect.rect.width / 2 + navBarRect.rect.width * progress;

        // 인디케이터 위치 업데이트
        if (playerIndicator != null)
        {
            playerIndicator.rectTransform.anchoredPosition = new Vector2(indicatorPosition, playerIndicator.rectTransform.anchoredPosition.y);
        }
    }

        private void InitializeNavigationBar()
    {
        // 네비게이션 바 활성화
        navigationBarImage.SetActive(true);

        // 기존 인디케이터 제거
        foreach (Transform child in navigationBarImage.transform)
        {
            Destroy(child.gameObject);
        }

        // 네비게이션 바의 RectTransform
        RectTransform navBarRect = navigationBarImage.GetComponent<RectTransform>();

        GameObject indicatorObject = Instantiate(playerIndicatorPrefab, navigationBarImage.transform);
        playerIndicator = indicatorObject.GetComponent<Image>();

        // 인디케이터 초기 위치 설정
        float initialIndicatorPosition = -navBarRect.rect.width / 2; // 왼쪽 끝으로 초기 위치 설정
        indicatorObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(initialIndicatorPosition, indicatorObject.GetComponent<RectTransform>().anchoredPosition.y);
        
    }

    [PunRPC]
    private void RPC_StartGameTimer()
    {
        StartGameTimer();
        gameRunning = true;
    }

    void OnInGameChatInputEndEdit(InputField input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return;

        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            SendInGameChat();
        }
    }

    public void SendInGameChat()
    {
        PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName + " : " + InGameChatInput.text);
        InGameChatInput.text = "";
        InGameChatInput.Select();
        InGameChatInput.ActivateInputField();
    }


    void RoomRenewal()
    {
        ListText.text = "";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            ListText.text += PhotonNetwork.PlayerList[i].NickName + ((i + 1 == PhotonNetwork.PlayerList.Length) ? "" : ", ");
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / " + PhotonNetwork.CurrentRoom.PlayerCount + "명 / " + PhotonNetwork.CurrentRoom.MaxPlayers + "최대";
    }

    #endregion

    #region 게임 종료
    public void OnPlayerReachedFinish()
    {
        if (playerFinishTimes.ContainsKey(PhotonNetwork.NickName))
            return;

        float finishTime = elapsedTime;
        playerFinishTimes[PhotonNetwork.NickName] = new PlayerData
        {
            nickName = PhotonNetwork.NickName,
            elapsedTime = finishTime,
            hasFinished = true,
            selectedCharacterIndex = selectedCharacterIndex
        };

        PV.RPC("RPC_UpdatePlayerFinishTime", RpcTarget.All, PhotonNetwork.NickName, finishTime, selectedCharacterIndex);

        if (!countdownStarted)
        {
            PV.RPC("RPC_StartCountdownToEnd", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_UpdatePlayerFinishTime(string playerName, float finishTime, int characterIndex)
    {
        if (!playerFinishTimes.ContainsKey(playerName))
        {
            playerFinishTimes[playerName] = new PlayerData
            {
                nickName = playerName,
                elapsedTime = finishTime,
                hasFinished = true,
                selectedCharacterIndex = characterIndex
            };
        }
    }

    [PunRPC]
    private void RPC_StartCountdownToEnd()
    {
        if (!countdownStarted)
        {
            countdownStarted = true;
            CountdownText.gameObject.SetActive(true); // 카운트다운 텍스트 활성화

            // 카운트다운 사운드 재생
            if (audioSource != null && countdownClip != null)
            {
                audioSource.PlayOneShot(countdownClip);
            }

            countdownCoroutine = StartCoroutine(CountdownToEndCoroutine());
        }
    }

    private IEnumerator CountdownToEndCoroutine()
    {
        int countdown = 10;

        while (countdown > 0)
        {
            // 카운트다운 텍스트를 갱신하고 모든 클라이언트에게 RPC를 통해 표시합니다.
            CountdownText.text = countdown.ToString();
            PV.RPC("UpdateCountdownText", RpcTarget.All, countdown);
            yield return new WaitForSeconds(1);
            countdown--;
        }
        CountdownText.text = "Game Over"; // 게임이 종료되면 초기값으로 설정
        PV.RPC("UpdateCountdownText", RpcTarget.All, -1); // 게임 종료를 알리기 위해 -1을 보냄
        yield return new WaitForSeconds(1f);

        PV.RPC("RPC_EndGame", RpcTarget.All); // 모든 클라이언트에서 EndGame을 호출
    }

    [PunRPC]
    void UpdateCountdownText(int countdown)
    {
        if (countdown >= 0)
        {
            CountdownText.text = countdown.ToString();
        }
        else
        {
            CountdownText.text = "Game Over";
        }
    }

    [PunRPC]
    void RPC_EndGame()
    {
        gameRunning = false;

        // 게임 종료 로직 구현
        TimeText.gameObject.SetActive(false);
        GameFinishPanel.SetActive(true); // 카운트다운이 끝나면 패널을 활성화
        CountdownText.gameObject.SetActive(false); // 카운트다운 텍스트 비활성화
        CountdownText.text = "10"; // 게임이 종료되면 초기값으로 설정
        countdownStarted = false; // 카운트다운 상태 초기화

        // 게임 종료 시 랭킹을 표시합니다.
        DisplayRanking();
        // 필요한 게임 종료 처리 추가

        // 이전 맵이 있다면 파괴
        if (currentMap != null)
        {
            Destroy(currentMap);
            currentMap = null;
        }

        // 모든 플레이어 프리팹 제거
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            Destroy(player);
        }

        // 타이머 초기화
        elapsedTime = 0f;

        // 네비게이션 바 비활성화
        navigationBarImage.SetActive(false);
        if (playerIndicator != null)
        {
            Destroy(playerIndicator.gameObject);
        }

        // 채팅 입력 필드 초기화 및 비활성화
        DeactivateChatInput();
        InGameChatInput.text = "";

        // 인게임 채팅 패널 비활성화
        ChatPanel.SetActive(false);

        // 인게임 채팅 내용 초기화
        foreach (Text chatText in InGameChatText)
        {
            chatText.text = "";
        }
    }

    private void DisplayRanking()
    {
        // playerFinishTimes의 값을 List로 변환하여 정렬합니다.
        List<PlayerData> sortedPlayerDataList = new List<PlayerData>(playerFinishTimes.Values);
        sortedPlayerDataList.Sort((a, b) => a.elapsedTime.CompareTo(b.elapsedTime));

        // 모든 클라이언트에게 순위 정보를 RPC를 통해 전달
        string[] rankInfos = new string[8]; // 8개의 텍스트 필드를 사용
        for (int i = 0; i < 8; i++)
        {
            if (i < sortedPlayerDataList.Count)
            {
                string rankText;
                // 도착하지 못한 플레이어인 경우
                if (!sortedPlayerDataList[i].hasFinished)
                {
                    rankText = (i + 1) + "등 " + sortedPlayerDataList[i].nickName + " (Retire)";
                }
                else
                {
                    // 도착한 플레이어인 경우
                    rankText = (i + 1) + "등 " + sortedPlayerDataList[i].nickName + " " + FormatTime(sortedPlayerDataList[i].elapsedTime);
                }
                rankInfos[i] = rankText;
            }
            else
            {
                rankInfos[i] = ""; // 남은 텍스트 필드는 빈 문자열로 설정
            }
        }

        PV.RPC("RPC_DisplayRanking", RpcTarget.All, rankInfos);
    }

    [PunRPC]
    private void RPC_DisplayRanking(string[] rankInfos)
    {
        for (int i = 0; i < Mathf.Min(rankInfos.Length, rankingTexts.Length); i++)
        {
            rankingTexts[i].text = rankInfos[i];
        }

        // 랭킹 텍스트가 사라지지 않도록 패널을 활성화 상태로 유지합니다.
        GameFinishPanel.SetActive(true);
        foreach (var rankingText in rankingTexts)
        {
            rankingText.gameObject.SetActive(true);
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        float seconds = time % 60F;
        return string.Format("{0}분 {1:00.0}초", minutes, seconds);
    }

    public void OnLeaveButtonClicked()
    {
        GameFinishPanel.SetActive(false);
        PhotonNetwork.LeaveRoom();
        ResetGameVariables();
    }
    #endregion

    // 특정 이벤트 패널을 표시하는 메서드
    public void ShowEventPanel(int eventIndex)
    {
        switch (eventIndex)
        {
            case 0:
                tripleJumpPanel.SetActive(true);
                break;
            case 1:
                doubleJumpPowerPanel.SetActive(true);
                break;
            case 2:
                maxSpeedPanel.SetActive(true);
                break;
        }

        // 5초 후 패널 비활성화
        StartCoroutine(HideEventPanelAfterDelay(eventIndex, 5f));
    }

    private IEnumerator HideEventPanelAfterDelay(int eventIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        switch (eventIndex)
        {
            case 0:
                tripleJumpPanel.SetActive(false);
                break;
            case 1:
                doubleJumpPowerPanel.SetActive(false);
                break;
            case 2:
                maxSpeedPanel.SetActive(false);
                break;
        }
    }
}