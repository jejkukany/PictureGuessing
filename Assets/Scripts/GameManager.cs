using System.Collections;
using System.IO;
using System.Linq;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject panel;
    public GameObject tiles;
    public GameObject gameCanvas;
    public GameObject menuCanvas;

    public Button startButton;
    public Button startPicButton;
    public Button showPicButton;
    public Button nextPicButton;

    public TMP_Text timerText;

    private Image _panelImage;
    private RectTransform _panelRectTransform;
    private RectTransform _tilesRectTransform;
    private GridLayoutGroup _tilesGrid;
    private Image[] _allTiles;
    private DirectoryInfo _path;

    // Button bools
    private bool _startPic;
    private bool _showPic;
    private bool _nextPic;
    private bool _exit;

    // Timer
    private float _timer;

    public void OpenFolder()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("", "", false);
        if (paths.Length <= 0) return;
        _path = new DirectoryInfo(paths[0]);
        startButton.interactable = true;
    }

    public void StartGame()
    {
        if (_path == null) return;
        StartCoroutine(Game());
    }

    public void ShowPic(bool next = false)
    {
        _showPic = true;
        showPicButton.interactable = false;
        if (!next) return;
        _nextPic = true;
        nextPicButton.interactable = false;
    }

    public void StartPic()
    {
        _startPic = true;
        startPicButton.interactable = false;
        showPicButton.interactable = true;
        nextPicButton.interactable = true;
    }

    public void ExitGame()
    {
        _exit = true;
        _startPic = true;
        _nextPic = true;
    }

    private void Start()
    {
        _panelImage = panel.GetComponent<Image>();
        _panelRectTransform = panel.GetComponent<RectTransform>();
        _tilesRectTransform = tiles.GetComponent<RectTransform>();
        _tilesGrid = tiles.GetComponent<GridLayoutGroup>();
        _allTiles = tiles.GetComponentsInChildren<Image>();
    }

    private IEnumerator Game()
    {
        _timer = 60 + 0.1f;
        timerText.text = "60";

        var images = _path.GetFiles().Where(s => ".png|.jpg|.jpeg|".Contains(s.Extension.ToLower() + "|")).ToArray();
        // 'Shuffle' the images array
        for (var t = 0; t < images.Length; t++)
        {
            var r = Random.Range(t, images.Length);
            var temp = images[r];
            images[r] = images[t];
            images[t] = temp;
        }

        gameCanvas.SetActive(true);
        menuCanvas.SetActive(false);
        foreach (var image in images)
        {
            // Set image as sprite
            _panelImage.sprite = Img2Sprite(image.FullName);

            // Adjust size of tiles
            var panelRect = _panelRectTransform.rect;
            var spriteRect = _panelImage.sprite.rect;
            float width;
            float height;

            if (spriteRect.width / panelRect.width < spriteRect.height / panelRect.height)
            {
                height = panelRect.height;
                width = spriteRect.width / spriteRect.height * height;
            }
            else
            {
                width = panelRect.width;
                height = spriteRect.height / spriteRect.width * width;
            }

            _tilesRectTransform.sizeDelta = new Vector2(width, height);
            _tilesGrid.cellSize = new Vector2(width / 7f, height / 5f);

            // 'Shuffle' the tiles array
            for (var t = 0; t < _allTiles.Length; t++)
            {
                var r = Random.Range(t, _allTiles.Length);
                var temp = _allTiles[r];
                _allTiles[r] = _allTiles[t];
                _allTiles[t] = temp;
            }

            startPicButton.interactable = true;
            showPicButton.interactable = false;
            nextPicButton.interactable = false;

            yield return new WaitUntil(() => _startPic);
            _startPic = false;
            InvokeRepeating(nameof(DecreaseTimer), 0, 0.1f);

            // Slowly disable tiles
            var speed = 50f / 60f;
            foreach (var tile in _allTiles)
            {
                tile.color = new Color(1, 1, 1, 0);
                yield return new WaitForSeconds(speed);
                if (_showPic)
                {
                    speed = 0.025f;
                    CancelInvoke(nameof(DecreaseTimer));
                }

                if (_exit) break;
            }

            if (_exit) break;

            CancelInvoke(nameof(DecreaseTimer));
            yield return new WaitUntil(() => _nextPic);
            _showPic = false;
            _nextPic = false;

            // Re-enable all tiles
            foreach (var tile in _allTiles)
            {
                tile.color = Color.white;
                yield return new WaitForSeconds(0.025f);
                if (_exit) break;
            }

            if (_exit) break;
        }

        menuCanvas.SetActive(true);
        gameCanvas.SetActive(false);
        _exit = false;
        _startPic = false;
        _showPic = false;
        _nextPic = false;
        CancelInvoke(nameof(DecreaseTimer));
        // Re-enable all tiles
        foreach (var tile in _allTiles)
        {
            tile.color = Color.white;
        }
    }

    private static Sprite Img2Sprite(string filePath, float pixelsPerUnit = 100f)
    {
        var spriteTexture = LoadTexture(filePath);
        return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height),
            new Vector2(0, 0), pixelsPerUnit);
    }

    private static Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var fileData = File.ReadAllBytes(filePath);
        var tex2D = new Texture2D(2, 2);
        return tex2D.LoadImage(fileData) ? tex2D : null;
    }

    private void DecreaseTimer()
    {
        if (_timer <= 0.1f)
        {
            CancelInvoke(nameof(DecreaseTimer));
            StartCoroutine(GameOver());
        }

        timerText.text = Mathf.FloorToInt(_timer).ToString();
    }

    private IEnumerator GameOver()
    {
        ShowPic();
        yield return new WaitForSeconds(5);
        ExitGame();
    }
}