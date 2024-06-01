using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ComputeUAVTexFlow : MonoBehaviour 
{
	// Variables públicas
	public ComputeShader computeShader;
	public Texture2D initTex; // Imagen inicial
	public Material collider; // Nuestro "lienzo"
	public Collider mc; // Necesario para detectar interacciones

	// Variables para el shader
	private int size_x;
	private int size_y;
	private int _kernel;
	private Vector2Int dispatchCount;

    // Variables para la detección de clicks
    private Camera cam;
    private RaycastHit hit;
    private Vector2 cursorPosition;
    private Vector2 defaultPosition = new Vector2(-9, -9); // Hay que ponerlo lejos
	private int mode = 0; // Por defecto está en modo pintar, por eso hay que colocar la posición lejos al inicio

	void Start () 
	{
		// Obtención de cámara
        cam = Camera.main;

		size_x = initTex.width;
		size_y = initTex.height;
		_kernel = computeShader.FindKernel("Main"); // Obtenemos nuestro kernel asociado

		// Creación de textura para el compute shader
		RenderTexture renderTexture = new RenderTexture (size_x, size_y, 0);
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		renderTexture.filterMode = FilterMode.Point;
		renderTexture.enableRandomWrite = true;
		renderTexture.Create ();
		
		Graphics.Blit(initTex, renderTexture); // De esta manera podemos escribir en nuestra textura inicial

		// Asignación de nueva textura al lienzo
		collider.SetTexture ("_MainTex", renderTexture);

		// Asignación de varaibles al shader
		computeShader.SetTexture (_kernel, "Result", renderTexture); // Asignamos la textura al kernel indicado (_kernel)
		computeShader.SetInt("_SizeX", size_x);
		computeShader.SetInt("_SizeY", size_y);

        uint threadX = 0;
        uint threadY = 0;
        uint threadZ = 0;
        computeShader.GetKernelThreadGroupSizes(_kernel, out threadX, out threadY, out threadZ);
		dispatchCount.x = Mathf.CeilToInt(size_x / threadX);
		dispatchCount.y = Mathf.CeilToInt(size_y / threadY);
	}

	void Update()
	{
        // Si se ha hecho click
        if ( Input.GetMouseButton(0) || Input.GetMouseButton(1) )
		{
			// Obtención de modos
			if( Input.GetMouseButton(0) && Input.GetMouseButton(1) ) mode = 2; // 2 = borrar obstáculo
			else if( !Input.GetMouseButton(0) && Input.GetMouseButton(1) ) mode = 1; // 1 = dibujar obstáculo
			else if( Input.GetMouseButton(0) && !Input.GetMouseButton(1) ) mode = 0; // 0 = dibujar grano de arena

			// Se obtiene la posición del cursor
			if( Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit) && hit.collider == mc )
			{
				if (cursorPosition != hit.textureCoord) cursorPosition = hit.textureCoord;
			}
			else
			{
				if (cursorPosition != defaultPosition) cursorPosition = defaultPosition;
			}
		}
        else
        {
            if (cursorPosition != defaultPosition) cursorPosition = defaultPosition;
        }

        // Ajustes y ejecución del shader
		computeShader.SetInt("_MouseMode", mode); // Se asigna el modo
        computeShader.SetVector("_MousePos", cursorPosition); // Se asigna la posición del cursor
		computeShader.SetFloat("_Time", Time.time); // Para el cálculo del aleatorio
		computeShader.Dispatch(_kernel, dispatchCount.x, dispatchCount.y, 1); // Se ejecuta
	}
}
