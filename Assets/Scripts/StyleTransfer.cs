using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Barracuda;
using Unity.Barracuda.ONNX;
using UnityEngine;
using UnityEngine.Serialization;

public class StyleTransfer : MonoBehaviour
{
    [SerializeField]
    private RenderTexture renderTexture;

    [SerializeField] private RenderTexture camRenderTexture;
    
    [SerializeField]
    private Texture2D inputTexture;
    
    [SerializeField]
    private Texture2D styleTexture;
    
    //====== MODEL ======//
    [SerializeField] private NNModel onnxModel;
    private Model _model;
    private IWorker _worker;

    private readonly Dictionary<string, Tensor> _inputs = new Dictionary<string, Tensor>();
    
    private void Awake()
    {
        LoadModel();
        InitializeSettings();
    }

    private void Start()
    {
        ExecuteModel();
    }

    private void LoadModel()
    {
        // 모델 로딩 코드
        Debug.Log("모델 로드 중...");
        _model = ModelLoader.Load(onnxModel);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, _model);
        
        Debug.Log("모델 로드 완료.");
    }

    private void InitializeSettings()
    {
        
        
        Debug.Log("초기 설정 완료.");
    }

    private void ExecuteModel()
    {
        // set camera texture to input
        // Tensor camInput = new Tensor(camRenderTexture, 3);
        
        // ===== Set Input ===== //
        Tensor input = new Tensor(camRenderTexture, 3);
        Tensor style = new Tensor(styleTexture, 3);
        _inputs["input_image"] = input;
        _inputs["input_style"] = style;
        
        // ===== Execute Model ===== //
        _worker.Execute(_inputs);
        
        // ===== Get Output ===== //
        Tensor output = _worker.PeekOutput();
        output.ToRenderTexture(renderTexture);
        
        // ===== Release Tensor ===== //
        input.Dispose();
        style.Dispose();
        output.Dispose();
    }

    private void Update()
    {
        ExecuteModel();
    }
}