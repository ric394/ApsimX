using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;

namespace Models.Factorial
{

    /// <summary>
    /// This class represents a series of paths and the same number of object values.
    /// Its sole purpose is to apply the object values to the model represented by the paths.
    /// If Specifications are specified then they are used instead of paths and values.
    /// </summary>
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.CompositeFactorPresenter")]
    public class CompositeFactor : Model, IReferenceExternalFiles
    {
        /// <summary>
        /// A list of models that have been passed into this composite factor 
        /// by the one edge case contructor that does that
        /// </summary>
        private List<IModel> _models { get; set; }

        /// <summary>Gets or sets the specification to create overides for a simulation.</summary>
        public List<string> Specifications { get; set; }

        /// <summary>Parameterless constrctor needed for serialisation</summary>
        public CompositeFactor()
        {
            _models = new List<IModel>();
            Specifications = new List<string>();
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(string name, string specification)
        {
            _models = new List<IModel>();
            Name = name;
            Specifications = new List<string>() {specification};
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(string name, string path, object value)
        {
            CreateSpecifications(path, value);
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(Factor parentFactor, string path, object value)
        {
            Parent = parentFactor;
            CreateSpecifications(path, value);
        }

        /// <summary>Contents of the CSV file</summary>
        [Display]
        public DataTable Data
        {
            get {
                return new DataTable();
            }
        }

        /// <summary>
        /// Apply this CompositeFactor to the specified simulation
        /// </summary>
        /// <param name="simulationDescription">A description of a simulation.</param>
        public void ApplyToSimulation(SimulationDescription simulationDescription)
        {
            List<CompositeFactorPair> pairs = ParseSpecifications();

            // Add a simulation override for each path / value combination.
            foreach(CompositeFactorPair pair in pairs)
            {
                if (pair.Value is INodeModel model)
                {
                    ModelReference reference = new ModelReference(model);
                    ReplaceCommand command = new ReplaceCommand(reference, pair.Path, true, ReplaceCommand.MatchType.NameOrType);
                    simulationDescription.AddOverride(command);
                }
                else
                {
                    SetPropertyCommand command = new SetPropertyCommand(pair.Path, "=", pair.Value.ToString(), multiple: true);
                    simulationDescription.AddOverride(command);
                }
            }

            if (Parent != null && !(Parent is Factors))
                simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor(Parent.Name, Name));
        }

        /// <summary>Return paths to all files referenced by this model.</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            List<CompositeFactorPair> pairs = ParseSpecifications();

            Simulations sims = Node.FindParent<Simulations>(recurse: true);
            List<string> values = new List<string>();
            foreach(CompositeFactorPair pair in pairs)
                if (pair.ValueType == typeof(string))
                    values.Add(pair.Value.ToString());
            IEnumerable<string> result = values.Where(str => File.Exists(PathUtilities.GetAbsolutePath(str, sims.FileName)));
            return result;
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            throw new NotImplementedException();
        }

        private void CreateSpecifications(string path, object value)
        {
            if (value is IModel model)
            {
                if (model is CompositeFactor factor)
                {
                    _models = new List<IModel>();
                    foreach(IModel child in factor.Children)
                        _models.Add(child);
                }
                else
                {
                    _models = new List<IModel>() {model};
                }
                Name = model.Name;
                Specifications = new List<string>() {$"{path}"};
            }
            else
            {
                _models = new List<IModel>();
                Name = value.ToString();
                Specifications = new List<string>() {$"{path}={value}"};
            }
        }

        /// <summary>
        /// Parse the specification into paths and values.
        /// </summary>
        private List<CompositeFactorPair> ParseSpecifications()
        {
            List<CompositeFactorPair> pairs = new List<CompositeFactorPair>();

            List<string> specifications = Specifications;
            //remove all blank lines
            specifications = specifications.Where(specification => specification.Length > 0).ToList();
            //remove all commented lines
            specifications = specifications.Where(specification => !specification.StartsWith("//")).ToList();

            if (specifications == null && specifications.Count == 0)
                return pairs;

            List<IModel> models = new List<IModel>();
            foreach(string specification in specifications)
            {
                string path = specification;
                object value = null;
                if (path.Contains("="))
                {
                    value = StringUtilities.SplitOffAfterDelimiter(ref path, "=").Trim();
                    if (value == null || value as string == "")
                        throw new Exception($"Error in composite factor {Name}: Unable to parse factor specification {specification}: No value was provided");
                    pairs.Add(new CompositeFactorPair(path.Trim(), value.ToString().Trim(), typeof(string)));
                }
                else
                {
                    // Find the model that we are to replace.
                    Experiment experiment = Parent.Node.FindParent<Experiment>(recurse: true);
                    Simulation baseSimulation = experiment.Node.FindChild<Simulation>(relativeTo: experiment);
                    IModel modelToReplace = baseSimulation.Node.Get(path) as IModel;
                    if (modelToReplace == null)
                        throw new Exception($"Error in CompositeFactor {Name}: Unable to find a model to replace from path '{path}'");

                    IEnumerable<IModel> modelsToSearch = Node.FindChildren<IModel>();
                    if (modelsToSearch.Count() == 0)
                        modelsToSearch = _models;

                    //Work out if any of the replacing models have the same type or share a non-imodel interface
                    List<Type> interfacesOfModel = typeof(Model).GetInterfaces().ToList();
                    interfacesOfModel.Add(typeof(IStructureDependency));
                    IEnumerable<Type> interfacesToReplace = modelToReplace.GetType().GetInterfaces().Except(interfacesOfModel);

                    List<IModel> possibleMatches = new List<IModel>();
                    foreach(IModel model in modelsToSearch)
                    {
                        if (model.GetType() == modelToReplace.GetType())
                            possibleMatches.Add(model);
                        else if (modelToReplace.GetType().IsAssignableFrom(model.GetType()))
                            possibleMatches.Add(model);
                        else
                        {
                            IEnumerable<Type> interfacesOfSearch = model.GetType().GetInterfaces().Except(interfacesOfModel);
                            //if this is a manager script, also get the interfaces from the class defined in the script
                            if (model is Manager manager)
                                interfacesOfSearch = interfacesOfSearch.AppendMany(manager.Script.GetType().GetInterfaces().Except(interfacesOfModel));

                            if (interfacesToReplace.Intersect(interfacesOfSearch).Any())
                                possibleMatches.Add(model);
                        }
                    }

                    //if no matches, throw
                    if (possibleMatches.Count() == 0)
                    {
                        throw new NullReferenceException($"Error in composite factor {Name}: Unable to parse factor specification {specification}: No children are of type {modelToReplace.GetType().Name}, so model {modelToReplace.Name} cannot be overriden.");
                    }
                    //If only one match, return that
                    else if (possibleMatches.Count() == 1)
                    {
                        value = possibleMatches.First();
                    }
                    //if multiple, try and match by name as well
                    else if (possibleMatches.Count() > 1) 
                    {
                        IModel match = possibleMatches.FirstOrDefault(m => m.Name.ToLower() == modelToReplace.Name.ToLower());
                        if (match == null) //if multiple matches, but none match on name, throw
                            throw new NullReferenceException($"Error in composite factor {Name}: Unable to parse factor specification {specification}: Multiple children of type {modelToReplace.GetType().Name} but none share a name with the model they replace. Ambiguous replacement has been prevented.");
                        else
                            value = match;
                    }

                    pairs.Add(new CompositeFactorPair(path.Trim(), value, typeof(IModel)));
                    models.Add(value as IModel);
                }
            }

            // If there are any child models which aren't being used as
            // a factor value (e.g. as a model replacement), throw an exception.
            IEnumerable<IModel> extraModels = Children.Except(models.OfType<IModel>());
            foreach (var model in extraModels)
                if (model.Enabled && !(model is Memo))
                    throw new InvalidOperationException($"Error in composite factor {Name}: Unused child models found: {string.Join(", ", extraModels.Select(m => m.Name))}");

            if (pairs.Count == 0)
                throw new InvalidOperationException($"Error in composite factor {Name}: Has no specifications");

            return pairs;
        }

        private class CompositeFactorPair
        {
            /// <summary>Path to change</summary>
            public string Path {get; private set;}

            /// <summary>Value/Model to change to</summary>
            public object Value {get; private set;}

            /// <summary>The type of the value</summary>
            public Type ValueType {get; private set;}

            public CompositeFactorPair(string path, object value, Type valueType)
            {
                Path = path;
                Value = value;
                ValueType = valueType;
            }
        }
    }
}