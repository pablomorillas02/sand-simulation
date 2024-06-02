using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SandSimulation : MonoBehaviour
{
    // Variables públicas
    public ComputeShader computeShader;
    public Texture2D initTex; // Imagen inicial
    public Material collider; // Nuestro "lienzo"
    public Collider mc; // Necesario para detectar interacciones
    public TextMesh text; // Para actualizar el texto

    // Compute buffer
    struct TextData
    {
        public uint granos;
        public uint obstaculos;
    }
    private TextData[] _data;
    private ComputeBuffer _computeBuffer;

    // Variables para el shader
    private int size_x;
    private int size_y;
    private int _sandKernel = 0;
    private int _countKernel = 1;
    private Vector2Int dispatchCount;

    // Variables para la detección de clicks
    private Camera cam;
    private RaycastHit hit;
    private Vector2 cursorPosition;
    private Vector2 defaultPosition = new Vector2(-9, -9); // Hay que ponerlo lejos
    private int mode = 0; // Por defecto está en modo pintar, por eso hay que colocar la posición lejos al inicio

    void Start()
    {
        // Obtención de cámara
        cam = Camera.main;

        size_x = initTex.width;
        size_y = initTex.height;
        _sandKernel = computeShader.FindKernel("SandKernel"); // Obtenemos nuestro kernel asociado
        _countKernel = computeShader.FindKernel("CountKernel");

        if (_sandKernel < 0 || _countKernel < 0)
        {
            Debug.LogError("Kernel not found!");
            return;
        }

        // Creación de textura para el compute shader
        RenderTexture renderTexture = new RenderTexture(size_x, size_y, 0);
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        Graphics.Blit(initTex, renderTexture); // De esta manera podemos escribir en nuestra textura inicial

        // Asignación de nueva textura al lienzo
        collider.SetTexture("_MainTex", renderTexture);

        // Asignación de varaibles al shader
        computeShader.SetTexture(_sandKernel, "Result", renderTexture); // Asignamos la textura al kernel indicado (_sandKernel)
        computeShader.SetTexture(_countKernel, "CountResult", renderTexture); // Trabajamos sobre la misma textura

        computeShader.SetInt("_SizeX", size_x);
        computeShader.SetInt("_SizeY", size_y);

        uint threadX = 0;
        uint threadY = 0;
        uint threadZ = 0;
        computeShader.GetKernelThreadGroupSizes(_sandKernel, out threadX, out threadY, out threadZ);
        dispatchCount.x = Mathf.CeilToInt(size_x / (float)threadX);
        dispatchCount.y = Mathf.CeilToInt(size_y / (float)threadY);

        // Ajuste del compute buffer
        _data = new TextData[1];
        _data[0].granos = 0;
        _data[0].obstaculos = 0;

        int stride = sizeof(uint) * 2;
        _computeBuffer = new ComputeBuffer(_data.Length, stride);
        _computeBuffer.SetData(_data); // Le pasamos el vector al compute buffer
        computeShader.SetBuffer(_countKernel, "data", _computeBuffer); // Lo asignamos al buffer del shader
    }

    void Update()
    {
        // Si se ha hecho click
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            // Obtención de modos
            if (Input.GetMouseButton(0) && Input.GetMouseButton(1)) mode = 2; // 2 = borrar obstáculo
            else if (!Input.GetMouseButton(0) && Input.GetMouseButton(1)) mode = 1; // 1 = dibujar obstáculo
            else if (Input.GetMouseButton(0) && !Input.GetMouseButton(1)) mode = 0; // 0 = dibujar grano de arena

            // Se obtiene la posición del cursor
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit) && hit.collider == mc)
            {
                cursorPosition = hit.textureCoord;
            }
            else
            {
                cursorPosition = defaultPosition;
            }
        }
        else
        {
            cursorPosition = defaultPosition;
        }

        // Ajustes y ejecución del shader
        computeShader.SetInt("_MouseMode", mode); // Se asigna el modo
        computeShader.SetVector("_MousePos", cursorPosition); // Se asigna la posición del cursor
        computeShader.SetFloat("_Time", Time.time); // Para el cálculo del aleatorio
        computeShader.Dispatch(_sandKernel, dispatchCount.x, dispatchCount.y, 1); // Se ejecuta el kernel de simulación
        computeShader.Dispatch(_countKernel, dispatchCount.x, dispatchCount.y, 1); // Se ejecuta el kernel de contar

        // Obtenemos los datos de vuelta de la GPU
        _computeBuffer.GetData(_data);

        // Actualizamos el texto
        float sand_count = _data[0].granos;
        float percent = ((float)_data[0].obstaculos/(size_x*size_y)) * 100f;

        text.text = "Granos: " + sand_count + "\nObstáculos (%): " + string.Format("{0:0.00}", percent) + "%";

        // Reset
        _data[0].granos = 0;
        _data[0].obstaculos = 0;
        _computeBuffer.SetData(_data);
    }

    void OnDestroy()
    {
        if (_computeBuffer != null)
        {
            _computeBuffer.Release();
        }
    }
}