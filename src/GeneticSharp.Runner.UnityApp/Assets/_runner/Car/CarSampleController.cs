﻿using GeneticSharp.Domain;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using UnityEngine;

namespace GeneticSharp.Runner.UnityApp.Car
{
    public class CarSampleController : SampleControllerBase
    {
        public int SecondsForEvaluation = 5;
        public int NumberOfSimultaneousEvaluations = 100;
        public Vector3 EvaluationDistance = new Vector3(0, 0, 2);
        public int VectorsCount = 8;
        public float VectorSize = 10;
        public Object CarPrefab;

        private CarFitness m_fitness;
        private Vector3 m_lastPosition = Vector3.zero;
        private PrefabPool m_carPool;

        protected override GeneticAlgorithm CreateGA()
        {
            m_fitness = new CarFitness(SecondsForEvaluation);
            var chromosome = new CarChromosome(VectorsCount, VectorSize);
            var crossover = new UniformCrossover();
            var mutation = new UniformMutation(true);
            var selection = new EliteSelection();
            var population = new Population(NumberOfSimultaneousEvaluations, NumberOfSimultaneousEvaluations, chromosome);
            var ga = new GeneticAlgorithm(population, m_fitness, selection, crossover, mutation);
            ga.Termination = new TimeEvolvingTermination(System.TimeSpan.FromDays(1));
            ga.TaskExecutor = new ParallelTaskExecutor
            {
                MinThreads = population.MinSize,
                MaxThreads = population.MaxSize * 2
            };
            ga.GenerationRan += delegate
            {
                m_lastPosition = Vector3.zero;
            };

            ga.MutationProbability = 0.2f;

            return ga;
        }

        protected override void StartSample()
        {
            m_carPool = new PrefabPool(CarPrefab);
        }

        protected override void UpdateSample()
        {
            // end evaluation.
            while (m_fitness.ChromosomesToEndEvaluation.Count > 0)
            {
                CarChromosome c;
                m_fitness.ChromosomesToEndEvaluation.TryTake(out c);
                var container = GameObject.Find(c.ID);
                var car = container.transform.GetChild(0);
                c.Distance = car.GetComponent<CarController>().Distance;

                m_carPool.Release(car.gameObject);
                GameObject.Destroy(container);
                c.Evaluated = true;
            }

            // in evaluation.
            while (m_fitness.ChromosomesToBeginEvaluation.Count > 0)
            {
                CarChromosome c;
                m_fitness.ChromosomesToBeginEvaluation.TryTake(out c);
                c.Evaluated = false;
                c.Distance = 0;
                 
                var container = new GameObject(c.ID);
                container.transform.position = m_lastPosition;
                m_lastPosition += EvaluationDistance;
                var car = m_carPool.Get(container.transform.position).GetComponent<CarController>();
                car.transform.SetParent(container.transform, false);
                car.SetChromosome(c);

            }
        }
    }
}