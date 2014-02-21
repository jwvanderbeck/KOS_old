﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using kOS.Binding;
using kOS.Command;
using kOS.Debug;
using kOS.Expression;
using kOS.Persistance;
using kOS.Utilities;

namespace kOS.Context
{
    public enum ExecutionState
    {
        NEW,
        DONE,
        WAIT
    };

    public enum SpecialKey
    {
        HOME,
        END,
        DELETE
    };

    public enum SystemMessage
    {
        CLEARSCREEN,
        SHUTDOWN,
        RESTART
    };

    public class ExecutionContext : IExecutionContext
    {
        public const int COLUMNS = 50;
        public const int ROWS = 36;
        public List<ICommand> CommandLocks = new List<ICommand>();
        public Dictionary<string, Expression.Expression> Locks = new Dictionary<string, Expression.Expression>();

        public ExecutionContext()
        {
            State = ExecutionState.NEW;
        }

        public ExecutionContext(IExecutionContext parent)
        {
            State = ExecutionState.NEW;
            ParentContext = parent;
        }

        public int Line { get; set; }

        public virtual IVolume SelectedVolume
        {
            get { return ParentContext != null ? ParentContext.SelectedVolume : null; }
            set { if (ParentContext != null) ParentContext.SelectedVolume = value; }
        }

        public virtual Vessel Vessel
        {
            get { return ParentContext != null ? ParentContext.Vessel : null; }
        }

        public virtual List<IVolume> Volumes
        {
            get { return ParentContext != null ? ParentContext.Volumes : null; }
        }

        public virtual IDictionary<string, Variable> Variables
        {
            get { return ParentContext != null ? ParentContext.Variables : null; }
        }

        public virtual List<KOSExternalFunction> ExternalFunctions
        {
            get { return ParentContext != null ? ParentContext.ExternalFunctions : null; }
        }

        public IExecutionContext ParentContext { get; set; }
        public IExecutionContext ChildContext { get; set; }
        public ExecutionState State { get; set; }

        public virtual void VerifyMount()
        {
            if (ParentContext != null) ParentContext.VerifyMount();
        }

        public bool KeyInput(char c)
        {
            return ChildContext != null ? ChildContext.Type(c) : Type(c);
        }


        public virtual bool Type(char c)
        {
            if (ChildContext != null) return ChildContext.Type(c);

            return false;
        }

        public virtual bool SpecialKey(kOSKeys key)
        {
            if (ChildContext != null) return ChildContext.SpecialKey(key);

            return false;
        }

        public virtual char[,] GetBuffer()
        {
            return (ChildContext != null) ? ChildContext.GetBuffer() : null;
        }

        public virtual void StdOut(string text)
        {
            if (ParentContext != null) ParentContext.StdOut(text);
        }

        public virtual void Put(string text, int x, int y)
        {
            if (ParentContext != null) ParentContext.Put(text, x, y);
        }

        public virtual void Update(float time)
        {
            // Process Command locks
            foreach (var command in new List<ICommand>(CommandLocks))
            {
                command.Update(time);
            }

            if (ChildContext != null)
            {
                if (ChildContext.State == ExecutionState.DONE)
                {
                    ChildContext = null;
                }
                else
                {
                    ChildContext.Update(time);
                }
            }
        }

        public virtual void Push(IExecutionContext newChild)
        {
            ChildContext = newChild;
        }

        public virtual bool Break()
        {
            if (ParentContext != null) return ParentContext.Break();

            return false;
        }

        public Variable FindVariable(string varName)
        {
            varName = varName.ToLower();

            var v = Variables.ContainsKey(varName) ? Variables[varName] : null;

            if (v == null && ParentContext != null)
            {
                v = ParentContext.FindVariable(varName);
            }

            if (v != null && Locks.ContainsKey(varName.ToUpper()))
            {
                v.Value = Locks[varName.ToUpper()].GetValue();
            }

            return v;
        }

        public Variable CreateVariable(string varName)
        {
            varName = varName.ToLower();

            var v = new Variable();
            Variables.Add(varName, v);
            return v;
        }

        public Variable FindOrCreateVariable(string varName)
        {
            varName = varName.ToLower();

            return FindVariable(varName) ?? CreateVariable(varName);
        }

        public virtual BoundVariable CreateBoundVariable(string varName)
        {
            return ParentContext.CreateBoundVariable(varName);
        }

        public virtual bool SwitchToVolume(int volID)
        {
            return ParentContext != null && ParentContext.SwitchToVolume(volID);
        }

        public virtual bool SwitchToVolume(string volName)
        {
            return ParentContext != null && ParentContext.SwitchToVolume(volName);
        }

        public virtual IVolume GetVolume(object volID)
        {
            if (volID is int)
            {
                if (Volumes.Count > (int) volID) return Volumes[(int) volID];
            }
            else if (volID is string)
            {
                var volName = volID.ToString().ToUpper();

                foreach (var targetVolume in Volumes.Where(targetVolume => targetVolume.Name.ToUpper() == volName))
                {
                    return targetVolume;
                }

                int outVal;
                if (int.TryParse((string) volID, out outVal))
                {
                    if (Volumes.Count > outVal) return Volumes[outVal];
                }
            }

            throw new KOSException("Volume '" + volID + "' not found");
        }

        public IExecutionContext GetDeepestChildContext()
        {
            return ChildContext == null ? this : ChildContext.GetDeepestChildContext();
        }

        public T FindClosestParentOfType<T>() where T : class, IExecutionContext
        {
            var casted = this as T;
            if (casted != null)
            {
                return casted;
            }

            return ParentContext == null ? null : ParentContext.FindClosestParentOfType<T>();
        }

        public void UpdateLock(string name)
        {
            var e = GetLock(name);
            if (e == null) return;
            var v = FindVariable(name);
            v.Value = e.GetValue();
        }

        public virtual Expression.Expression GetLock(string name)
        {
            if (Locks.ContainsKey(name.ToUpper()))
            {
                return Locks[name.ToUpper()];
            }
            return ParentContext == null ? null : ParentContext.GetLock(name);
        }

        public virtual void Lock(ICommand command)
        {
            CommandLocks.Add(command);
        }

        public virtual void Lock(string name, Expression.Expression expression)
        {
            name = name.ToLower();

            FindOrCreateVariable(name);

            if (!Locks.ContainsKey(name.ToUpper()))
            {
                Locks.Add(name.ToUpper(), expression);
            }
        }

        public virtual void Unlock(ICommand command)
        {
            CommandLocks.Remove(command);
            if (ParentContext != null) ParentContext.Unlock(command);
        }

        public virtual void Unlock(string name)
        {
            name = name.ToLower();

            if (Locks.ContainsKey(name.ToUpper()))
            {
                Locks.Remove(name.ToUpper());
            }
            else if (ParentContext != null)
            {
                ParentContext.Unlock(name);
            }
        }

        public virtual void UnlockAll()
        {
            Locks.Clear();
            if (ParentContext != null) ParentContext.UnlockAll();
        }

        public virtual void Unset(string name)
        {
            if (Variables.ContainsKey(name.ToLower()))
            {
                Variables.Remove(name.ToLower());
            }
            else if (ParentContext != null)
            {
                ParentContext.Unset(name.ToLower());
            }
        }

        public virtual void UnsetAll()
        {
            for (var i = 0; i < Variables.Count; i++)
            {
                var currvar = Variables.ElementAt(i);

                if (!(currvar.Value is BoundVariable))
                {
                    Variables.Remove(currvar.Key);
                }
            }
            if (ParentContext != null) ParentContext.UnsetAll();
        }

        public bool ParseNext(ref string buffer, out string cmd, ref int lineCount, out int lineStart)
        {
            lineStart = -1;

            for (var i = 0; i < buffer.Length; i++)
            {
                var c = buffer.Substring(i, 1);

                if (lineStart < 0 && Regex.Match(c, "\\S").Success) lineStart = lineCount;
                else if (c == "\n") lineCount++;

                switch (c)
                {
                    case "\"":
                        i = Utils.FindEndOfstring(buffer, i + 1);
                        if (i == -1)
                        {
                            cmd = "";
                            return false;
                        }
                        break;
                    case ".":
                        {
                            int pTest;
                            if (i == buffer.Length - 1 || int.TryParse(buffer.Substring(i + 1, 1), out pTest) == false)
                            {
                                cmd = buffer.Substring(0, i);
                                buffer = buffer.Substring(i + 1).Trim();

                                return true;
                            }
                        }
                        break;
                    case "{":
                        i = Utils.BraceMatch(buffer, i);
                        if (i == -1)
                        {
                            cmd = "";
                            return false;
                        }
                        // Do you see a period after this right brace? If not, let's just pretend there is one ok?
                        if (!buffer.Substring(i + 1).StartsWith("."))
                        {
                            cmd = buffer.Substring(0, i + 1);
                            buffer = buffer.Substring(i + 1).Trim();

                            return true;
                        }
                        break;
                }
            }

            cmd = "";
            return false;
        }

        public virtual void SendMessage(SystemMessage message)
        {
            if (ParentContext != null) ParentContext.SendMessage(message);
        }

        public virtual int GetCursorX()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : -1;
        }

        public virtual int GetCursorY()
        {
            return ChildContext != null ? ChildContext.GetCursorY() : -1;
        }

        public virtual object CallExternalFunction(string name, string[] parameters)
        {
            return ParentContext != null ? ParentContext.CallExternalFunction(name, parameters) : null;
        }

        public virtual bool FindExternalFunction(string name)
        {
            return ParentContext != null && ParentContext.FindExternalFunction(name);
        }

        public virtual void OnSave(ConfigNode node)
        {
            var contextNode = new ConfigNode("context");

            contextNode.AddValue("context-type", GetType().ToString());

            if (ChildContext != null)
            {
                ChildContext.OnSave(contextNode);
            }

            node.AddNode(contextNode);
        }

        public virtual void OnLoad(ConfigNode node)
        {
        }

        public virtual string GetVolumeBestIdentifier(IVolume selectedVolume)
        {
            return ParentContext != null ? ParentContext.GetVolumeBestIdentifier(selectedVolume) : "";
        }
    }
}