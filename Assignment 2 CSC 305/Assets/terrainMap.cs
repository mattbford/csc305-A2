//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class terrainMap : MonoBehaviour
{

    public Material terrainMaterial;
    public GameObject tree;
    public GameObject house;


    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        renderer.material = terrainMaterial;
        GenerateTerrainMap();
    }

    // Update is called once per frame
    void Update()
    {

    }

    float interpolation_function(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    float Mix(float x, float y, float z)
    {
        return (1 - z) * x + z * y;
    }

    //code written with help from Perlin1DGenerator
    float [,] generatePerlinNoise (int num_sample, int frequency)
    {
        Vector2[,] gradients = new Vector2[frequency + 1, frequency + 1];

        for (int i = 0; i < frequency + 1; ++i)
        {
            for (int j = 0; j < frequency + 1; ++j)
            {
                Vector2 rand_vector = new Vector2(Random.value * 2 - 1, Random.value * 2 - 1);
                gradients[i, j] = rand_vector.normalized;
            }
        }

        /*Vector2[] edge_centers = new Vector2[4];
        edge_centers[0] = (new Vector2(0, 1)).normalized;
        edge_centers[1] = (new Vector2(0, -1)).normalized;
        edge_centers[2] = (new Vector2(1, 0)).normalized;
        edge_centers[3] = (new Vector2(-1, 0)).normalized;

        for (int i = 0; i < frequency + 1; ++i)
        {
            for (int j = 0; j < frequency + 1; ++j)
            {
                float roll = Random.value;
                if (roll < 0.25f) gradients[i, j] = edge_centers[0];
                if (roll < 0.5f) gradients[i, j] = edge_centers[1];
                if (roll < 0.75f) gradients[i, j] = edge_centers[2];
                else gradients[i, j] = edge_centers[3];
            }            
        }*/

        float[,] noise = new float[num_sample, num_sample];
        float period = 1.0f / frequency;
        float step = 1.0f / num_sample;
        for(int i = 0; i < num_sample; ++i)
        {
            for (int j = 0; j < num_sample; ++j)
            {
                // Math from slides + Perlin1DGenerator
                float location_period_x = step * i / period;
                float location_period_y = step * j / period;
                int cell_x = Mathf.FloorToInt(location_period_x);
                int cell_y = Mathf.FloorToInt(location_period_y);
                float in_cell_location_x = location_period_x - cell_x;
                float in_cell_location_y = location_period_y - cell_y;
                Vector2 position = new Vector2(in_cell_location_x, in_cell_location_y);

                Vector2 a = position - (new Vector2(0, 0));
                Vector2 b = position - (new Vector2(1, 0));
                Vector2 c = position - (new Vector2(0, 1));
                Vector2 d = position - (new Vector2(1, 1));
                float s = Vector2.Dot(gradients[cell_x, cell_y], a);
                float t = Vector2.Dot(gradients[cell_x + 1, cell_y], b);
                float u = Vector2.Dot(gradients[cell_x, cell_y + 1], c);
                float v = Vector2.Dot(gradients[cell_x + 1, cell_y + 1], d);

                float st = Mix(s, t, interpolation_function(in_cell_location_x));
                float uv = Mix(u, v, interpolation_function(in_cell_location_x));
                noise[i, j] = Mix(st, uv, interpolation_function(in_cell_location_y));
                if (noise[i, j] < 0.0f)
                {
                    noise[i, j] = 0.0f;
                }
                //Debug.Log(noise[i,j]);
            }            
        }
        

        return noise;
    }

    int[] object_placement(float[,] perlinHeight)
    {
        int obj_i = Random.Range(1, 250);
        int obj_j = Random.Range(1, 250);

        while (perlinHeight[obj_i, obj_j] <= 0.1)
        {
            obj_i = Random.Range(1, 250);
            obj_j = Random.Range(1, 250);
        }

        int[] obj = new int[2];
        obj[0] = obj_i;
        obj[1] = obj_j;

        return obj;
    }

    void GenerateTerrainMap()
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        mesh.Clear();

        //subdivision = how many squares per row/col
        int subdivision = 250;
        int stride = subdivision + 1;
        int num_vert = stride * stride;
        int num_tri = subdivision * subdivision * 2;

        indices = new int[num_tri * 3];
        int index_ptr = 0;
        for (int i = 0; i < subdivision; i++)
        {
            for (int j = 0; j < subdivision; j++)
            {
                int quad_corner = i * stride + j;
                indices[index_ptr] = quad_corner;
                indices[index_ptr + 1] = quad_corner + stride;
                indices[index_ptr + 2] = quad_corner + stride + 1;
                indices[index_ptr + 3] = quad_corner;
                indices[index_ptr + 4] = quad_corner + stride + 1;
                indices[index_ptr + 5] = quad_corner + 1;
                index_ptr += 6;
            }
        }

        Debug.Assert(index_ptr == indices.Length);

        const float xz_start = -5;
        const float xz_end = 5;
        float step = (xz_end - xz_start) / (float)(subdivision);
        vertices = new Vector3[num_vert];
        uvs = new Vector2[num_vert];

        //generate perlin noise here
        float[,] perlinHeight = generatePerlinNoise(250, 5);

        // gets random seed for tree & house locations
        int[] tree_i_j = object_placement(perlinHeight);
        int[] house_i_j = object_placement(perlinHeight);

        for (int i = 0; i < stride-1; i++)
        {
            for (int j = 0; j < stride-1; j++)
            {
                // This is where parabola is generated. This must be changed to accomadate perlin noise grid
                bool show_backface = false;
                float cur_x;
                float cur_z;
                //i don't know how this happened(showing back faces)
                if (show_backface)
                {
                    cur_x = xz_start + i * step;
                    cur_z = xz_start + j * step;
                }
                else
                {
                    cur_x = xz_start + j * step;
                    cur_z = xz_start + i * step;
                }

                float cur_y = perlinHeight[i, j];
                vertices[i * stride + j] = new Vector3(cur_x, cur_y, cur_z);

                if (i == tree_i_j[0] && j == tree_i_j[1])
                {
                    tree = Instantiate(tree, vertices[i * stride + j], Quaternion.identity) as GameObject;
                    tree.transform.localScale = new Vector3(3, 3, 3);
                    // makes tree right-side up (weird starting values)
                    tree.transform.Rotate(0, 140, -90);
                    //adjust to normal
                }

                if (i == house_i_j[0] && j == house_i_j[1])
                {
                    house = Instantiate(house, vertices[i * stride + j], Quaternion.identity) as GameObject;
                    house.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                }

                /*if (perlinHeight[i, j] > 0.4 && tree_place == 0 && i > 0 && j > 0 && i < 205 && j < 250)
                {
                    tree_place = 1;
                    tree = Instantiate(tree, vertices[i * stride + j], Quaternion.identity) as GameObject;
                    tree.transform.localScale = new Vector3(3,3,3);
                    // makes tree right-side up (weird starting values)
                    tree.transform.Rotate(0, 140, -90);
                    //adjust to normal
                    
                    
                }*/

            }
        }

        for(int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
    }
}
