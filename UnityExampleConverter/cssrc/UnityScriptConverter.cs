using Boo.Lang;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.MetaProgramming;
using System;
using System.Collections;
using UnityScript;
using UnityScript.Steps;

namespace UnityExampleConverter
{
    [Serializable]
    public class UnityScriptConverter
    {
        protected UnityScriptCompiler _compiler;
        public CompilerInputCollection Input
        {
            get
            {
                return Parameters.Input;
            }
        }
        public CompilerReferenceCollection References
        {
            get
            {
                return Parameters.References;
            }
        }
        private UnityScriptCompilerParameters Parameters
        {
            get
            {
                return _compiler.Parameters;
            }
        }
        public UnityScriptConverter()
        {
            _compiler = new UnityScriptCompiler();
            CompilerPipeline compilerPipeline = UnityScriptCompiler.Pipelines.RawParsing();
            compilerPipeline.Add(new ResolveMonoBehaviourType());
            compilerPipeline.Add(new ApplySemantics());
            compilerPipeline.Add(new ApplyDefaultVisibility());
            compilerPipeline.Add(new TypeInference());
            compilerPipeline.Add(new FixScriptClass());
            compilerPipeline.Add(new RemoveEmptyMethods());
            compilerPipeline.Add(new AddImports());
            compilerPipeline.Add(new RenameArrayDeclaration());
            compilerPipeline.Add(new RenameIEnumerator());
            compilerPipeline.Add(new TransformKnownCalls());
            compilerPipeline.Add(new GenericTransformer());
            _compiler.Parameters.ScriptMainMethod = "Example";
            _compiler.Parameters.Pipeline = compilerPipeline;
            var imports = _compiler.Parameters.Imports;
            imports.Add("UnityEngine");
            imports.Add("System.Collections");
        }
        public void AddReference(string reference)
        {
            References.Add(Parameters.LoadAssembly(reference, true));
        }
        public CompileUnit[] Run()
        {
            CompilerContext compilerContext = _compiler.Run();
            if (((ICollection)compilerContext.Errors).Count != 0)
            {
                throw new CompilationErrorsException(compilerContext.Errors);
            }
            CompileUnit compileUnit = compilerContext.CompileUnit;
            CompileUnit node = compileUnit.CloneNode();
            return new CompileUnit[]
		{
			ApplyCSharpTransformer(compileUnit),
			ApplyBooTransformer(node)
		};
        }
        private CompileUnit ApplyBooTransformer(CompileUnit node)
        {
            node.Accept(new BooTransformer());
            return node;
        }
        private CompileUnit ApplyCSharpTransformer(CompileUnit node)
        {
            node.Accept(new CSharpTransformer());
            return node;
        }
    }
}
