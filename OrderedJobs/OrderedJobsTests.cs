using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OrderedJobs
{
    public class JobTests
    {
        public void ParseJobNoDependency()
        {
            var job = Job.Parse("a =>");

            job.Name.ShouldBe("a");
            job.HasDependency().ShouldBe(false);
        }

        public void ParseJobWithDependency()
        {
            var job = Job.Parse("a => b");

            job.Name.ShouldBe("a");
            job.HasDependency().ShouldBe(true);
        }
    }

    public class Job
    {
        public static Job Parse(string job)
        {
            var match = Regex.Match(job, @"(?'Name'\w) => ?(?'Dependency'\w?)");
            if (match.Groups["Dependency"].Length > 0)
                return new Job(match.Result("${Name}"), match.Result("${Dependency}"));

            return new Job(match.Result("${Name}"));
        }

        private Job(string name, string dependency = null)
        {
            if (name == dependency) throw new CircularDependency();

            Name = name;
            Dependency = dependency;
        }

        public string Name { get; private set; }
        public string Dependency { get; private set; }

        public bool HasDependency()
        {
            return Dependency != null;
        }

        public override string ToString()
        {
            return Name + " => " + Dependency;
        }
    }

    public class OrderedJobsTests
    {
        private static string Sort(string jobs)
        {
            var sortedJobs = Sort(Parse(jobs));

            return sortedJobs.Aggregate("", (result, job) => result + job.Name);
        }

        private static IEnumerable<Job> Parse(string jobs)
        {
            return jobs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Job.Parse);
        }

        private static IEnumerable<Job> Sort(IEnumerable<Job> jobs)
        {
            var list = jobs.ToList();
            var processed = new HashSet<Job>();
            foreach (var job in list.Where(job => !processed.Contains(job)))
            {
                var current = job;
                var dependencies = new Stack<Job>();
                while (current.HasDependency())
                {
                    current = list.First(j => j.Name == current.Dependency);
                    if (current == job) throw new CircularDependency();
                    dependencies.Push(current);
                }

                foreach (var dependency in dependencies.Where(dependency => !processed.Contains(dependency)))
                {
                    processed.Add(dependency);
                    yield return dependency;
                }
                processed.Add(job);
                yield return job;
            }
        }






        public void EmptyJobList()
        {
            Sort("")
                .ShouldBe("");
        }

        public void SingleJobA()
        {
            Sort("a =>")
                .ShouldBe("a");
        }

        public void SingleJobB()
        {
            Sort("b =>")
                .ShouldBe("b");
        }

        public void MultipleJobsWithNoDependencies()
        {
            Sort(
@"a =>
b =>
c =>")
                .ShouldBe("abc");
        }

        public void MultipleJobsWithOneDependency()
        {
            Sort(
@"a =>
b => c
c =>")
                .ShouldBe("acb");
        }

        public void MultipleJobsWithTwoDependencies()
        {
            Sort(
@"a => b
b => c
c =>")
                .ShouldBe("cba");
        }

        public void MultipleJobsWithMultipleDependencies()
        {
            Sort(
@"a =>
b => c
c => f
d => a
e => b
f =>")
                .ShouldBe("afcbde");
        }

        public void PreventSelfReferencingDependency()
        {
            Should.Throw<CircularDependency>(() =>
                Sort(
@"a =>
b =>
c => c"));
        }

        public void PreventCircularDependencies()
        {
            Should.Throw<CircularDependency>(() =>
                Sort(
@"a =>
b => c
c => f
d => a
e =>
f => b"));
        }
    }

    public class CircularDependency : Exception
    {
    }
}
