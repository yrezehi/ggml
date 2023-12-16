﻿using ggmlcs.Native.Binding;
using ggmlcs.Native.Binding.Structs;

// reference llama.cpp official: https://github.com/ggerganov/llama.cpp/blob/8a5be3bd5885d79ad84aadf32bb8c1a67bd43c19/examples/simple/simple.cpp#L42
namespace ggmlcs.Native
{
    public class LLama : IDisposable
    {
        private LLamaContext Context { get; set; }
        private LLamaModel Model { get; set; }

        private LLamaContextParams ContextParams { get; set; } = new LLamaContextParams();
        private LLamaModelParams ModelParams { get; set; } = new LLamaModelParams();

        private LLama(LLamaContext context, LLamaModel model) =>
            (Context, Model) = (context, model);

        public void Run(string prompt)
        {

            if (string.IsNullOrEmpty(prompt)) {
                throw new ArgumentException(message: "No prompt was provided!");
            }

            var embeddings = new LLamaToken[prompt.Length + 1];
            LLamaMethods.llama_tokenize(Model, prompt, prompt.Length, embeddings, embeddings.Length);

            if(embeddings.Length == 0) {
                throw new MemberAccessException(message: "Embedding tokens were empty!");
            }

            if(embeddings.Length > LLamaContextParams.n_ctx){
                throw new MemberAccessException(message: "Embedding tokens is larger than allowed context!");
            }

            int n_past = 0;
            int n_remain = 512; // TODO: need att
            int n_consumed = 0;
            int n_session_consumed = 0;
            int n_past_guidance = 0;
            int guidance_offset = 0;

            while (n_remain != 0)
            {
                for(int index = 0; index < embeddings.Length; index += LLamaContextParams.n_batch)
                {
                    int n_eval = embeddings.Length - index;

                    if (n_eval > LLamaContextParams.n_batch)
                    {
                        n_eval = LLamaContextParams.n_batch;
                    }

                }
            }

        }

        public static LLama CreateInstance(string path)
        {
            if (!Path.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            LLamaModelParams modelParams = LLamaMethods.llama_model_default_params();
            LLamaModel model = LLamaMethods.llama_load_model_from_file(path, modelParams);

            if(model == null)
            {
                throw new MemberAccessException(message: $"Unable to load model {path}");
            }


        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
