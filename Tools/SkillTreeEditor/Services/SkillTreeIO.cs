using Newtonsoft.Json;
using SkillTreeEditor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkillTreeEditor.Services;

public static class SkillTreeIO
{
    // 与 mod 内加载器 RenderNodeLoader.Load() 一致：固定读取这 5 个 index 文件
    public static readonly string[] IndexFiles =
    [
        "index.json",
        "index_droneupg.json",
        "index_droneportupg.json",
        "index_reactorupg.json",
        "index_torsoupg.json",
    ];

    public static SkillTreeProject LoadProject(string skillTreeFolder)
    {
        if (string.IsNullOrWhiteSpace(skillTreeFolder))
            throw new ArgumentException("skillTreeFolder is empty");

        var dir = Path.GetFullPath(skillTreeFolder);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException(dir);

        var project = new SkillTreeProject { SkillTreeFolder = dir };

        // 1) load index*.json (render nodes)
        foreach (var fn in IndexFiles)
        {
            var path = Path.Combine(dir, fn);
            if (!File.Exists(path))
            {
                project.IndexFileToNodes[path] = new List<SkillTreeRenderNode>();
                continue;
            }

            var text = File.ReadAllText(path);
            var nodes = JsonConvert.DeserializeObject<List<SkillTreeRenderNode>>(text) ?? new List<SkillTreeRenderNode>();
            foreach (var n in nodes) n.__sourceIndexFile = path;
            project.IndexFileToNodes[path] = nodes;
        }

        // 2) load skills/**/*.json (skill nodes)
        var skillsDir = Path.Combine(dir, "skills");
        if (Directory.Exists(skillsDir))
        {
            foreach (var file in Directory.EnumerateFiles(skillsDir, "*.json", SearchOption.AllDirectories))
            {
                SkillNode? node = null;
                try
                {
                    node = JsonConvert.DeserializeObject<SkillNode>(File.ReadAllText(file));
                }
                catch
                {
                    // ignore broken json; leave to user fix manually
                }

                if (node == null || string.IsNullOrWhiteSpace(node.skillID))
                    continue;

                node.__sourceSkillFile = file;
                project.SkillIdToNode[node.skillID] = node;
            }
        }

        return project;
    }

    public static void SaveProject(SkillTreeProject project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (string.IsNullOrWhiteSpace(project.SkillTreeFolder)) throw new InvalidOperationException("Project not loaded.");

        // write index files
        foreach (var kv in project.IndexFileToNodes)
        {
            var indexPath = kv.Key;
            var nodes = kv.Value ?? new List<SkillTreeRenderNode>();

            // 清理 editor-only 字段（JsonIgnore 已处理），这里只是保证 source 设置回去
            foreach (var n in nodes) n.__sourceIndexFile = indexPath;

            var text = JsonConvert.SerializeObject(nodes, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, text);
        }

        // write skills
        foreach (var node in project.SkillIdToNode.Values)
        {
            if (string.IsNullOrWhiteSpace(node.__sourceSkillFile))
                continue; // 暂不自动创建新文件（避免误写）；需要时可以后续补

            var text = JsonConvert.SerializeObject(node, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(node.__sourceSkillFile!)!);
            File.WriteAllText(node.__sourceSkillFile!, text);
        }
    }

    public static string BuildSkillNodeFilePath(SkillTreeProject project, string subFolder, string skillId)
    {
        var skillsRoot = Path.Combine(project.SkillTreeFolder, "skills");
        var safeFolder = string.IsNullOrWhiteSpace(subFolder) ? "custom" : subFolder.Trim();

        // Skill.DronePortUpg.JetJump.Lv0 -> skill_droneportupg_jetjump_lv0.json
        var fileName = "skill_" + skillId.Replace('.', '_').ToLowerInvariant() + ".json";
        return Path.Combine(skillsRoot, safeFolder, fileName);
    }

    public static string? TryFindIllustrationFolder(string skillTreeFolder)
    {
        // skilltree 的同级目录下通常有 illustrations/
        // .../mod/skilltree -> .../mod/illustrations
        try
        {
            var modDir = Directory.GetParent(Path.GetFullPath(skillTreeFolder))?.FullName;
            if (modDir == null) return null;
            var ill = Path.Combine(modDir, "illustrations");
            return Directory.Exists(ill) ? ill : null;
        }
        catch
        {
            return null;
        }
    }
}
