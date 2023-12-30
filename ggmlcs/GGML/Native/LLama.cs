﻿using GGML.Native.Binding;
using GGML.Native.Binding.Definitions;
using GGML.Native.DLLs;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

// reference llama.cpp official: https://github.com/ggerganov/llama.cpp/blob/8a5be3bd5885d79ad84aadf32bb8c1a67bd43c19/examples/simple/simple.cpp#L42

namespace GGML.Native
{
    public unsafe class LLama : IDisposable
    {
        private LLamaContext Context { get; set; }
        private LLamaModel Model { get; set; }

        private LLamaContextParams ContextParams { get; set; } = new LLamaContextParams();
        private LLamaModelParams ModelParams { get; set; } = new LLamaModelParams();

        private LLama(LLamaContext context, LLamaModel model, LLamaContextParams contextParams) =>
            (Context, Model, ContextParams) = (context, model, contextParams);

        public static LLama CreateInstance(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            DLLLoader.LibraryLoad();

            LLamaMethods.llama_backend_init();

            LLamaModelParams modelParams = LLamaMethods.llama_model_default_params();
            modelParams.vocab_only = false;

            LLamaModel model = LLamaMethods.llama_load_model_from_file(path, modelParams);

            if (model == LLamaModel.Zero)
            {
                throw new MemberAccessException(message: $"Unable to load model {path}");
            }

            LLamaContextParams contextParams = LLamaMethods.llama_context_default_params();

            contextParams.seed = 1234;
            contextParams.n_ctx = 2048;

            contextParams.n_threads_batch = 8;
            contextParams.n_threads_batch = 64;

            LLamaContext context = LLamaMethods.llama_new_context_with_model(model, contextParams);

            if (context == LLamaModel.Zero)
            {
                throw new MemberAccessException(message: $"Unable to load context {path}");
            }

            return new LLama(context, model, contextParams);
        }

        public void Infer(string prompt)
        {

            Console.Write(prompt);

            LLamaToken[] tokens = new LLamaToken[prompt.Length];
            int tokensSize = LLamaMethods.llama_tokenize(Model, prompt, prompt.Length, tokens, tokens.Length);
            Array.Resize(ref tokens, tokensSize);

            int n_len = 32;

            int n_ctx = LLamaMethods.llama_n_ctx(Context);
            int n_kv_req = tokens.Length + (n_len - tokens.Length);

            if (n_kv_req > n_ctx)
            {
                throw new MemberAccessException(message: $"KV Cache is not big enough!");
            }

            LLamaBatch batch = LLamaMethods.llama_batch_init(512, 0, 1);

            for (int index = 0; index < tokens.Length; index++)
            {
                LLamaMethods.llama_batch_add(ref batch, tokens[index], index, new[] { 0 }, false);
            }

            batch.logits[batch.n_tokens - 1] = 1;

            if (LLamaMethods.llama_decode(Context, batch) != 0)
            {
                throw new MemberAccessException(message: $"Failed to decode batch!");
            }

            int n_cur = batch.n_tokens;

            while (n_cur <= n_len)
            {
                int n_vocab = LLamaMethods.llama_n_vocab(Model);
                float* logits = LLamaMethods.llama_get_logits_ith(Context, batch.n_tokens - 1);

                LLamaTokenData[] candidates = new LLamaTokenData[n_vocab];

                for (LLamaToken token = 0; token < n_vocab; token++)
                {
                    candidates[token] = new LLamaTokenData(token, logits[token], 0.0f);
                }

                LLamaTokenDataArray candidates_p = new LLamaTokenDataArray(candidates, candidates.Length, false);

                LLamaToken token_id = LLamaMethods.llama_sample_token_greedy(Context, ref candidates_p);

                if (token_id == LLamaMethods.llama_token_eos(Model) || n_cur == n_len)
                {
                    break;
                }

                char[] buffer = new char[8];

                var result = LLamaMethods.llama_token_to_piece(Model, token_id, buffer, buffer.Length);

                if (result < 0)
                {
                    Array.Resize(ref buffer, -result);
                    LLamaMethods.llama_token_to_piece(Model, token_id, buffer, buffer.Length);
                    result = -result;
                }
                else
                {
                    Array.Resize(ref buffer, result);
                }

                string toReturn = new(buffer, 0, result);
                byte[] dataAsWindows1252 = Encoding.UTF8.GetBytes(toReturn);

                string correctlyInterpretedString = Encoding.UTF8.GetString(dataAsWindows1252);
                Console.Write(correctlyInterpretedString);

                batch.n_tokens = 0;

                LLamaMethods.llama_batch_add(ref batch, token_id, n_cur, new[] { 0 }, true);

                n_cur += 1;

                if (LLamaMethods.llama_decode(Context, batch) != 0)
                {
                    throw new MemberAccessException(message: $"Failed to decode batch!");
                }
            }

            LLamaMethods.llama_batch_free(batch);

            LLamaMethods.llama_free(Context);

            LLamaMethods.llama_free_model(Model);

            LLamaMethods.llama_backend_free();
        }

        public void Dispose() { throw new NotImplementedException(); }
    }
}