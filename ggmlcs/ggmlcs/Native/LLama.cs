﻿using ggmlcs.Native.Binding;
using ggmlcs.Native.Binding.Entities;
using ggmlcs.Native.Binding.Params;
using ggmlcs.Native.Libs;
using System;
using System.IO;
using System.Runtime.InteropServices;

// reference llama.cpp official: https://github.com/ggerganov/llama.cpp/blob/8a5be3bd5885d79ad84aadf32bb8c1a67bd43c19/examples/simple/simple.cpp#L42

namespace ggmlcs.Native
{
    public unsafe class LLama : IDisposable
    {
        private LLamaContext Context { get; set; }
        private LLamaModel Model { get; set; }

        private LLamaContextParams ContextParams { get; set; } = new LLamaContextParams();
        private LLamaModelParams ModelParams { get; set; } = new LLamaModelParams();

        private LLama(LLamaContext context, LLamaModel model) =>
            (Context, Model) = (context, model);

        public static LLama CreateInstance(string path)
        {
            if (!Path.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            LibLoader.LibraryLoad();

            LLamaMethods.llama_backend_init();

            LLamaModelParams modelParams = LLamaMethods.llama_model_default_params();
            LLamaModel model = LLamaMethods.llama_load_model_from_file(path, modelParams);

            if(model == nint.Zero)
            {
                throw new MemberAccessException(message: $"Unable to load model {path}");
            }

            LLamaContextParams contextParams = LLamaMethods.llama_context_default_params();
            LLamaContext context = LLamaMethods.llama_new_context_with_model(model, contextParams);

            if(context == nint.Zero)
            {
                throw new MemberAccessException(message: $"Unable to load context {path}");
            }

            return new LLama(context, model);
        }

        public void Infer(string prompt)
        {
            LLamaToken[] tokens = new LLamaToken[prompt.Length + 1];
            LLamaMethods.llama_tokenize(Model, prompt, prompt.Length, tokens, tokens.Length);

            int n_len = 32;

            int n_ctx = LLamaMethods.llama_n_ctx(Context);
            int n_kv_req = tokens.Length + (n_len - tokens.Length);

            if (n_kv_req > n_ctx)
            {
                throw new MemberAccessException(message: $"KV Cache is not big enough!");
            }

            foreach (var token in tokens)
            {
                char[] buffer = new char[8];
                Console.Error.Write(LLamaMethods.llama_token_to_piece(Model, token, buffer.ToArray(), buffer.Length));
            }

            LLamaBatch batch = LLamaMethods.llama_batch_init();

            for (int index = 0; index < tokens.Length; index++)
            {
                LLamaMethods.llama_batch_add(ref batch, tokens[index], index, new[] { 0 }, false);
            }

            batch.logits[batch.n_tokens] = (byte) 1;

            if (LLamaMethods.llama_decode(ctx, batch) != 0)
            {
                LOG_TEE("%s: llama_decode() failed\n", __func__);
                return 1;
            }

            int n_cur = batch.n_tokens;

            while (n_cur <= n_len)
            {
                int n_vocab = LLamaMethods.llama_n_vocab(Model);
                float* logits = LLamaMethods.llama_get_logits_ith(Context, batch.n_tokens);

                LLamaTokenData[] candidates = new LLamaTokenData[n_vocab];

                for (LLamaToken token = 0; token < n_vocab; token++) {
                    candidates[token] = new LLamaTokenData(token, logits[token], 0.0f);
                }

                LLamaTokenDataArray candidates_p = new LLamaTokenDataArray(candidates, candidates.Length, false);

                LLamaToken token_id = LLamaMethods.llama_sample_token_greedy(Context, candidates_p);

                LLamaMethods.llama_batch_add(ref batch, token_id, n_cur, new[] { 0 }, true);

                n_cur += 1;
            }

            LLamaMethods.llama_batch_free(batch);

            LLamaMethods.llama_free(Context);
            LLamaMethods.llama_free_model(Context);

            LLamaMethods.llama_free_model(Model);
            LLamaMethods.llama_backend_free();
        }

        public void Dispose() { throw new NotImplementedException(); }
    }
}
