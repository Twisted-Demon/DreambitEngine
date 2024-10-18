using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class CutsceneManager : Component
{
    private readonly Logger<CutsceneManager> _logger = new();
    
    private Queue<CutsceneAction> _actions = [];
    private float _timer;
    private bool _isWaitingForInput;
    
    public bool IsCutsceneActive { get; private set; }

    public void LoadCutscene(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var actionsList = JsonConvert.DeserializeObject<List<CutsceneAction>>(json);

            _actions.Clear();
            _timer = 0;
            _isWaitingForInput = false;

            foreach (var action in actionsList)
            {
                _actions.Enqueue(action);
            }

            IsCutsceneActive = true;
        }
        catch (Exception e)
        {
            _logger.Warn("Could not load cutscene {0}", filePath);
            _logger.Error(e.Message);
            throw;
        }
    }

    public override void OnUpdate()
    {
        if (!IsCutsceneActive) return;

        if (_actions.Count == 0)
        {
            IsCutsceneActive = false;
            return;
        }

        var action = _actions.Peek();

        switch (action.Action)
        {
            case "move":
                HandleMoveAction(action);
                break;
            case "wait":
                HandleWaitAction(action);
                break;
        }
    }

    public void HandleMoveAction(CutsceneAction action)
    {
        var entityName = action.Entity;
        var entity = Entity.FindByName(entityName);

        if (entity == null)
        {
            _logger.Warn("Could not find entity named {0}. Moving to next cutscene action", entityName);
            return;
        }

        var mover = entity.AttachComponent<Mover>();
        var destination = new Vector3(action.Destination[0], action.Destination[1], 0);
        var speed = action.MoveSpeed;

        if (mover.MoveTo(destination, speed))
            StartNextAction();
    }

    public void HandleWaitAction(CutsceneAction action)
    {
        
        _timer += Time.DeltaTime;
        if (!(_timer >= action.Duration)) return;
        
        _timer = 0;
        StartNextAction();
    }

    public void StartNextAction()
    {
        if (_actions.Count == 0)
        {
            _logger.Trace("No more actions, cutscene ended");
            return;
        }
        
        _actions.Dequeue();
    }
}