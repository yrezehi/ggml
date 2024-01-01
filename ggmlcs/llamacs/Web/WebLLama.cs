﻿using LLamacs.Native.Binding.Definitions.Context;
using LLamacs.Native.Binding.Definitions.Model;
using LLamacs.Native.Binding;
using LLamacs.Native.DLLs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLamacs.Web.Clients;

namespace LLamacs.Web
{
    public class WebLLama : ILLama<LLamaClient>
    {
        private LLamaContext Context { get; set; }
        private LLamaModel Model { get; set; }

        private LLamaContextParams ContextParams { get; set; } = new LLamaContextParams();
        private LLamaModelParams ModelParams { get; set; } = new LLamaModelParams();

        private WebLLama(LLamaContext context, LLamaModel model, LLamaContextParams contextParams) =>
            (Context, Model, ContextParams) = (context, model, contextParams);

        public static WebLLama CreateInstance(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            DLLLoader.LibraryLoad();

            LLamaMethodsHandler.BackendInit();

            LLamaModelParams modelParams = LLamaModelParams.Default();

            LLamaModel model = LLamaMethodsHandler.LoadModelFromFile(path, modelParams);

            LLamaContextParams contextParams = LLamaContextParams.Default();

            LLamaContext context = LLamaMethodsHandler.NewContextWithModel(model, contextParams);

            return new WebLLama(context, model, contextParams);
        }

        public void Infer(LLamaClient client) {
            
        }
    }
}