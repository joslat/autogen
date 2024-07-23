﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// LMStudioAgent.cs

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoGen.OpenAI;
using OpenAI;
using OpenAI.Chat;

namespace AutoGen.LMStudio;

/// <summary>
/// agent that consumes local server from LM Studio
/// </summary>
/// <example>
/// [!code-csharp[LMStudioAgent](../../sample/AutoGen.BasicSamples/Example08_LMStudio.cs?name=lmstudio_example_1)]
/// </example>
public class LMStudioAgent : IAgent
{
    private readonly GPTAgent innerAgent;

    public LMStudioAgent(
        string name,
        LMStudioConfig config,
        string systemMessage = "You are a helpful AI assistant",
        float temperature = 0.7f,
        int maxTokens = 1024,
        IEnumerable<ChatTool>? functions = null,
        IDictionary<string, Func<string, Task<string>>>? functionMap = null)
    {
        var client = ConfigOpenAIClientForLMStudio(config);
        var chatClient = client.GetChatClient("llm"); // model name doesn't matter for LM Studio
        innerAgent = new GPTAgent(
            name: name,
            systemMessage: systemMessage,
            chatClient: chatClient,
            temperature: temperature,
            maxTokens: maxTokens,
            functions: functions,
            functionMap: functionMap);
    }

    public string Name => innerAgent.Name;

    public Task<IMessage> GenerateReplyAsync(
        IEnumerable<IMessage> messages,
        GenerateReplyOptions? options = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return innerAgent.GenerateReplyAsync(messages, options, cancellationToken);
    }

    private OpenAIClient ConfigOpenAIClientForLMStudio(LMStudioConfig config)
    {
        // create uri from host and port
        var uri = config.Uri;
        var option = new OpenAIClientOptions()
        {
            Endpoint = uri,
        };

        return new OpenAIClient("api-key", option);
    }

    private sealed class CustomHttpClientHandler : HttpClientHandler
    {
        private Uri _modelServiceUrl;

        public CustomHttpClientHandler(Uri modelServiceUrl)
        {
            _modelServiceUrl = modelServiceUrl;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // request.RequestUri = new Uri($"{_modelServiceUrl}{request.RequestUri.PathAndQuery}");
            var uriBuilder = new UriBuilder(_modelServiceUrl);
            uriBuilder.Path = request.RequestUri.PathAndQuery;
            request.RequestUri = uriBuilder.Uri;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
