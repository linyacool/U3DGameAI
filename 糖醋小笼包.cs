using System.Linq;
using TuringCup;
using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;

public class DefaultTeam : AIBase
{

    public DefaultTeam(int a_index) : base(a_index) { }

    public override string TeamName
    {
        get
        {
            return "糖醋小笼包";
        }
    }

    public override Character ChooseCharacter
    {
        get
        {
            return Character.Tyor;
        }
    }

    private float shootRange = 9;
    private bool first_step = true;
    private bool step_1, step_2, step_3, step_4, step_5;
    private int myindex;
    private int attacked_barrels = 0;
    private float end_x, end_z;
    private const float DEVIATION = (float)0.5;
    private bool isArrive = false;
    private char Area;
    private int enemie_index = -1;
    private bool kill = false;
    private const float PI = Mathf.PI;
    private float death_x, death_z;
    private bool eat = false;

    private float[,] YL = { { 0f, 100f }, { 100f, 100f }, { 100f, 0f }, { 0f, 0f } };

    protected float Moderate(float ang)
    {
        while (ang < 0) ang += 2 * PI;
        while (ang >= 2 * PI) ang -= 2 * PI;
        return ang;
    }
    protected override void Act(JObject state)
    {
        var me = state["me"];
        float x = (float)me["pos"]["x"];
        float z = (float)me["pos"]["z"];
        float hp = (float)me["hp"];
        float time = (float)state["time"];
        //UnityEngine.Debug.Log(time);
        if (first_step)
        {
            if (x < 50 && z < 50)
            {
                Move(75, 25);
                end_x = 75;
                end_z = 25;
                Area = 'C';
            }
            if (x < 50 && z > 50)
            {
                Move(25, 25);
                end_x = 25;
                end_z = 25;
                Area = 'D';
            }
            if (x > 50 && z < 50)
            {
                Move(75, 75);
                end_x = 75;
                end_z = 75;
                Area = 'B';
            }
            if (x > 50 && z > 50)
            {
                Move(25, 75);
                end_x = 25;
                end_z = 75;
                Area = 'A';
            }
            attacked_barrels = 0;
            step_1 = true;
            step_2 = false;
            step_3 = false;
            step_4 = false;
            step_5 = false;
            first_step = false;
        }


        var Jtargets = state["barrels"] as JArray;
        var Jenemies = state["enemies"] as JArray;
        var targets = state["barrels"];
        var enemies = state["enemies"];
        var Jpick = state["pickups"] as JArray;
        var pick = state["pickups"];
        var misc = state["misc"] as JArray;

        if (Mathf.Abs(x - end_x) < DEVIATION && Mathf.Abs(z - end_z) < DEVIATION)
            isArrive = true;
        else
            isArrive = false;

        if (step_1)
        {
            if (!isArrive)
            {
                UseSkill(0);
            }
            else
            {
                go_another_area(state);
            }
            if (Jenemies.Count > 0 && ((int)Jenemies[0]["state"] & 0x01) == 0)
            {
                enemie_index = (int)Jenemies[0]["index"];
                Move((float)Jenemies[0]["pos"]["x"], (float)Jenemies[0]["pos"]["z"]);
                end_x = (float)Jenemies[0]["pos"]["x"];
                end_z = (float)Jenemies[0]["pos"]["z"];
                step_1 = false;
                step_2 = true;
                step_3 = false;
                step_4 = false;
                step_5 = false;
            }
        }

        /* if (enemie_index==-1){
             if (kill){
                 first_step=false;
                 step_1 = false;
                 step_4 = false;
                 step_3 = true;
                 step_2 = false;
             }
             else {
                 first_step=false;
                 step_1 = true;
                 step_4 = false;
                 step_3 = true;
                 step_2 = false;
             }
         }*/





        if (step_2)
        {
            int now = -1;
            if (Jenemies.Count > 0)
            {
                for (int i = 0; i < Jenemies.Count; ++i)
                {
                    if ((int)Jenemies[i]["index"] == enemie_index)
                    {
                        now = i;
                        break;
                    }
                }

                //判断敌人是否死亡
                if (((int)Jenemies[now]["state"] & 0x01) != 0)
                {
                    step_1 = false;
                    step_4 = false;
                    step_3 = true;
                    step_2 = false;
                    step_5 = false;
                    kill = true;
                    return;
                }

                //转换目标，还未接近目标
                if (Distance(me, enemies[now]) >= 16.3 * 16.3)
                {
                    for (int i = 0; i < Jenemies.Count; ++i)
                    {
                        if (((int)Jenemies[i]["state"] & 0x01) == 0)
                            if (Mathf.Abs((float)Jenemies[i]["hp"] - (float)Jenemies[now]["hp"]) <= 15f ||
                                (float)Jenemies[i]["hp"] < (float)Jenemies[now]["hp"])
                            {
                                float tmp1 = (float)Jenemies[i]["score"] - 2 * (float)Jenemies[i]["hp"];
                                float tmp2 = (float)Jenemies[now]["score"] - 2 * (float)Jenemies[now]["hp"];
                                if (tmp1 > tmp2)
                                {
                                    now = i;
                                    enemie_index = (int)Jenemies[i]["index"];
                                }
                            }
                    }
                }

                //血量不够，时间不够我复活，怂。
                if (time >= 82 && hp <= 32)
                {
                    step_1 = false;
                    step_2 = false;
                    step_3 = false;
                    step_4 = false;
                    step_5 = true;
                }

                //时间不够，血量不健康，杀不死人
                if (time > 110 && hp <= 50 && (int)Jenemies[now]["hp"] >= hp + 10)
                {
                    step_1 = false;
                    step_2 = false;
                    step_3 = false;
                    step_4 = false;
                    step_5 = true;
                }

                //时间不够。三人中分数第一，血量很少。躲避，
                bool ImBest = true;
                if (Jenemies.Count >= 2)
                {
                    for (int i = 0; i < Jenemies.Count; i++)
                        if ((float)me["score"] < (float)Jenemies[i]["score"]) ImBest = false;
                }
                if (Jenemies.Count <= 1) ImBest = false;
                if (time > 105 && hp <= 50 && ImBest)
                {
                    step_1 = false;
                    step_2 = false;
                    step_3 = false;
                    step_4 = false;
                    step_5 = true;
                }



                float x1, z1, x2, z2, x3, z3, ang, r;
                if (Jenemies.Count == 1)
                {//只有一个人的情况，考虑考虑道具问题。
                    int p_tmp = 0;
                    for (int i = 1; i < Jpick.Count; i++)
                    {
                        if (Distance(me, pick[p_tmp]) > Distance(me, pick[i]))
                            p_tmp = i;
                    }
                    bool fight = true;
                    if (Jpick.Count > 0)
                        if ((int)Jpick[p_tmp]["type"] == 1)
                            if (Distance(me, pick[p_tmp]) + Distance(pick[p_tmp], enemies[now]) <= 2f * Distance(me, enemies[now]))
                                fight = false;
                    if (Jpick.Count > 0)
                        if ((int)Jpick[p_tmp]["type"] == 0)
                            if (Distance(me, pick[p_tmp]) + Distance(pick[p_tmp], enemies[now]) <= 1.44f * Distance(me, enemies[now]))
                                fight = false;
                    if (Jpick.Count == 0 || fight)
                    {
                        if (Jpick.Count == 0)
                        {//没有道具，找个好位置。
                            x1 = (float)Jenemies[now]["pos"]["x"];
                            z1 = (float)Jenemies[now]["pos"]["z"];
                            x2 = 50f;//x2 = YL[(int)Jenemies[now]["index"], 0];
                            z2 = 50f;//z2 = YL[(int)Jenemies[now]["index"], 1];
                            ang = Mathf.Atan2(z2 - z1, x2 - x1);
                            ang = Moderate(ang);
                            r = 1.5f;
                            x3 = x1 - r * Mathf.Cos(ang);
                            z3 = z1 - r * Mathf.Sin(ang);
                        }
                        else
                        {//有道具，并不能优先我去拾起道具，阻挡对面拾起。
                            x1 = (float)Jenemies[now]["pos"]["x"];
                            z1 = (float)Jenemies[now]["pos"]["z"];
                            x2 = (float)Jpick[p_tmp]["pos"]["x"];//x2 = YL[(int)Jenemies[now]["index"], 0];
                            z2 = (float)Jpick[p_tmp]["pos"]["z"];//z2 = YL[(int)Jenemies[now]["index"], 1];
                            ang = Mathf.Atan2(z2 - z1, x2 - x1);
                            ang = Moderate(ang);
                            r = 1.5f;
                            x3 = x1 + r * Mathf.Cos(ang);
                            z3 = z1 + r * Mathf.Sin(ang);
                        }
                    }
                    else
                    {//有道具，我应该优先去拿道具。
                        x3 = (float)Jpick[p_tmp]["pos"]["x"];
                        z3 = (float)Jpick[p_tmp]["pos"]["z"];
                    }
                }
                else if (Jenemies.Count == 2)
                {
                    int y = 1 - now;
                    x1 = (float)Jenemies[now]["pos"]["x"];
                    z1 = (float)Jenemies[now]["pos"]["z"];
                    x2 = (float)Jenemies[y]["pos"]["x"];
                    z2 = (float)Jenemies[y]["pos"]["z"];
                    ang = Mathf.Atan2(z2 - z1, x2 - x1);
                    ang = Moderate(ang);
                    ang += PI / 6f;//防止机械师
                    r = 1.5f;
                    x3 = x1 - r * Mathf.Cos(ang);
                    z3 = z1 - r * Mathf.Sin(ang);
                }
                else
                {
                    int y, yy;
                    if (now == 0) { y = 1; yy = 2; }
                    else if (now == 1) { y = 0; yy = 2; }
                    else { y = 0; yy = 1; }
                    x1 = (float)Jenemies[now]["pos"]["x"];
                    z1 = (float)Jenemies[now]["pos"]["z"];
                    x2 = (float)Jenemies[y]["pos"]["x"] + (float)Jenemies[yy]["pos"]["x"];
                    x2 = x2 / 2.0f;
                    z2 = (float)Jenemies[y]["pos"]["z"] + (float)Jenemies[yy]["pos"]["z"];
                    z2 = z2 / 2.0f;
                    ang = Mathf.Atan2(z2 - z1, x2 - x1);
                    ang = Moderate(ang);
                    r = 1.5f;
                    x3 = x1 - r * Mathf.Cos(ang);
                    z3 = z1 - r * Mathf.Sin(ang);
                }

                if ((int)Jenemies[now]["type"] == 0)
                {//火女
                    //Move(x3,z3);
                    //考虑到雷大招的延迟效果，见到就放大招比较合适。
                    //可能血量低的时候，不用大招
                    if (Distance(me, enemies[now]) <= 16 * 16)
                        UseSkill(1, enemie_index);
                    //不见到人，考虑不用Q
                    if (Distance(me, enemies[now]) <= 3 * 3)
                        UseSkill(0);

                    //躲避大招，/****//有时候不需要
                    //UseSkill(0);

                }
                else if ((int)Jenemies[now]["type"] == 1)
                {//机械师
                    //没过多处理
                    if (Distance(me, enemies[now]) <= 16 * 16)
                        UseSkill(1, enemie_index);
                    if (Distance(me, enemies[now]) <= 3 * 3)
                        UseSkill(0);


                }
                else if ((int)Jenemies[now]["type"] == 2)
                {//雷
                    //没过多处理
                    if (Distance(me, enemies[now]) <= 16 * 16)
                        UseSkill(1, enemie_index);
                    if (Distance(me, enemies[now]) <= 3 * 3)
                        UseSkill(0);
                    if (isLeaf(me, enemies[now]) && Distance(me, enemies[now]) <= 16)
                    {
                        if (((int)Jenemies[now]["state"] & 0x04) != 0)
                            UseSkill(0);
                    }

                }
                else if ((int)Jenemies[now]["type"] == 3)
                {

                    /*if (Distance(me, enemies[now]) <= 16 * 16)
                        UseSkill(1, enemie_index);
                    if (Distance(me, enemies[now]) <= 3 * 3)
                        UseSkill(0);*/
                    //判断冰刃 和 火焰雨
                    int Mi = -1;
                    for (int i = 0; i < misc.Count; i++)
                        if ((string)misc[i]["type"] == "iceedge")
                        {
                            Mi = i;
                            break;
                        }
                    int Ni = -1;
                    for (int i = 0; i < misc.Count; i++)
                        if ((string)misc[i]["type"] == "snowstorm")
                        {
                            Ni = i;
                            break;
                        }


                    float Dis = Mathf.Sqrt(Distance(me, enemies[now]));
                    if (Distance(me, enemies[now]) <= 16 * 16) UseSkill(1, enemie_index);
                    //if (Dis <= 5.6 && Mi != -1 && Ni == -1) UseSkill(0);//攻击躲技能
                    //if (Dis <= 3.0 && Ni != -1 && ((int)state["me"]["state"] & 0x10) != 0) UseSkill(0);//冰冻能攻击到;

                    UseSkill(0);

                    //转圈躲Q
                    if (Dis < 12.0f)
                    {
                        x1 = (float)Jenemies[now]["pos"]["x"];
                        z1 = (float)Jenemies[now]["pos"]["z"];
                        x2 = (float)me["pos"]["x"];
                        z2 = (float)me["pos"]["z"];
                        ang = Mathf.Atan2(z2 - z1, x2 - x1);
                        ang = Moderate(ang);
                        ang += PI / 6.0f;
                        float R = Mathf.Sqrt(Distance(me, enemies[now]));
                        if (Ni == -1)
                        {
                            if (((int)me["state"] & 0x04) != 0)
                                r = Mathf.Max(1.5f, R - 1);
                            else r = Mathf.Max(5.5f, R - 1);
                        }
                        else
                        {
                            if (((int)me["state"] & 0x04) != 0)
                                r = Mathf.Max(1.5f, R - 1);
                            else r = Mathf.Max(8.8f, R - 1);
                        }


                        x3 = x1 + r * Mathf.Cos(ang);
                        z3 = z1 + r * Mathf.Sin(ang);
                    }


                }


                //判断火女大招
                int Hi = -1;
                for (int i = 0; i < misc.Count; i++)
                    if ((string)misc[i]["type"] == "meteorite" && ((float)misc[i]["pos"]["x"] - x) * ((float)misc[i]["pos"]["x"] - x) + ((float)misc[i]["pos"]["z"] - z) * ((float)misc[i]["pos"]["z"] - z) < 10 * 10)
                    {
                        Hi = i;
                        break;
                    }
                if (Hi != -1)
                {
                    int sender = (int)misc[Hi]["sender"];
                    int tmp = -1;
                    for (int i = 0; i < Jenemies.Count; i++)
                    {
                        if ((int)Jenemies[i]["index"] == sender)
                        {
                            tmp = i;
                        }
                    }
                    float lastDam = (4.0f - (float)Jenemies[tmp]["cd"][1]) * 12.0f;
                    if ((float)Jenemies[now]["hp"] >= lastDam + 14.0 || (tmp == now && (float)Jenemies[now]["hp"] > 32))
                    {
                        x1 = (float)misc[Hi]["pos"]["x"];
                        z1 = (float)misc[Hi]["pos"]["z"];
                        //x2 = (float)Jenemies[now]["pos"]["x"];
                        //z2 = (float)Jenemies[now]["pos"]["z"];
                        x2 = (float)me["pos"]["x"];
                        z2 = (float)me["pos"]["z"];
                        ang = Mathf.Atan2(z2 - z1, x2 - x1);
                        ang = Moderate(ang);
                        x3 = x1 + 11.5f * Mathf.Cos(ang);
                        z3 = z1 + 11.5f * Mathf.Sin(ang);
                        UseSkill(0);
                    }
                }

                Move(x3, z3);
                end_x = x3;
                end_z = z3;


            }

            //没有人为什么step_1==true?
            else //(Jenemies.Count == 0)
            {
                go_another_area(state);
                step_2 = false;
                step_3 = true;
                step_1 = false;
                step_4 = false;
                step_5 = false;
            }
        }

        //判断自己死亡
        if (((int)state["me"]["state"] & 0x01) != 0)
        {
            death_x = (x + 50f) / 2f;
            death_z = (z + 50f) / 2f;
            step_4 = true;
            step_1 = false;
            step_2 = false;
            step_3 = false;
            step_5 = false;
        }


































        if (step_3)
        {
            //float temp_dis = (float)99999.0;
            //int now_i = -1;
            if (Jenemies.Count == 0)
                eat = true;
            if (eat)
            {
                int target = -1;
                if (Jtargets.Count > 0)
                {
                    int minest = 0;
                    float minDis = Distance(state["me"], targets[0]);
                    for (int i = 1; i < Jtargets.Count; ++i)
                    {
                        if (Distance(state["me"], targets[i]) < minDis)
                        {
                            minest = i;
                            minDis = Distance(state["me"], targets[i]);
                        }
                    }
                    target = minest;
                    Move((float)Jtargets[minest]["pos"]["x"], (float)Jtargets[minest]["pos"]["z"]);
                    end_x = (float)Jtargets[minest]["pos"]["x"];
                    end_z = (float)Jtargets[minest]["pos"]["z"];
                }
                if (target != -1)
                {
                    if (Distance(me, targets[target]) <= 3 * 3)
                    {
                        UseSkill(0);
                    }
                }
                if (Jpick.Count > 0)
                {
                    float minDis_p = 99999;
                    int pick_i = 0;
                    for (int i = 0; i < Jpick.Count; ++i)
                    {
                        if (Distance(state["me"], pick[i]) < minDis_p)
                        {
                            pick_i = i;
                            minDis_p = Distance(state["me"], pick[i]);
                        }
                    }
                    end_x = (float)Jpick[pick_i]["pos"]["x"];
                    end_z = (float)Jpick[pick_i]["pos"]["z"];
                    Move(end_x, end_z);
                }
                if (Jtargets.Count == 0 && Jpick.Count == 0 && isArrive)
                {
                    go_another_area(state);
                }
            }
            if (Jenemies.Count == 1)
            {
                if (((int)Jenemies[0]["state"] & 0x01) != 0)
                {
                    eat = true;
                    return;
                }
                eat = false;
                enemie_index = (int)Jenemies[0]["index"];
                step_1 = false;
                step_2 = true;
                step_3 = false;
                step_4 = false;
                step_5 = false;
            }
            if (Jenemies.Count == 2)
            {
                float hp0 = (float)Jenemies[0]["hp"];
                float hp1 = (float)Jenemies[1]["hp"];
                int isTyor0 = ((int)Jenemies[0]["type"] == 2 ? 1 : 0);
                int isTyor1 = ((int)Jenemies[1]["type"] == 2 ? 1 : 0);
                float dis0 = Mathf.Sqrt(Distance(me, enemies[0]));
                float dis1 = Mathf.Sqrt(Distance(me, enemies[1]));
                float val0 = hp0 + dis0 + isTyor0 * 10;
                float val1 = hp1 + dis1 + isTyor1 * 10;
                if (((int)Jenemies[0]["state"] & 0x01) == 0 && ((int)Jenemies[1]["state"] & 0x01) == 0)
                {
                    if (val0 <= val1)
                    {
                        enemie_index = (int)Jenemies[0]["index"];
                    }
                    else
                    {
                        enemie_index = (int)Jenemies[1]["index"];
                    }
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                }
                if (((int)Jenemies[0]["state"] & 0x01) == 0 && ((int)Jenemies[1]["state"] & 0x01) != 0)
                {
                    enemie_index = (int)Jenemies[0]["index"];
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                }
                if (((int)Jenemies[0]["state"] & 0x01) != 0 && ((int)Jenemies[1]["state"] & 0x01) == 0)
                {
                    enemie_index = (int)Jenemies[1]["index"];
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                }
                if (((int)Jenemies[0]["state"] & 0x01) != 0 && ((int)Jenemies[1]["state"] & 0x01) != 0)
                {
                    eat = true;
                }
            }
            if (Jenemies.Count == 3)
            {
                float hp0 = (float)Jenemies[0]["hp"];
                float hp1 = (float)Jenemies[1]["hp"];
                float hp2 = (float)Jenemies[2]["hp"];
                int isTyor0 = ((int)Jenemies[0]["type"] == 2 ? 1 : 0);
                int isTyor1 = ((int)Jenemies[1]["type"] == 2 ? 1 : 0);
                int isTyor2 = ((int)Jenemies[2]["type"] == 2 ? 1 : 0);
                float dis0 = Mathf.Sqrt(Distance(me, enemies[0]));
                float dis1 = Mathf.Sqrt(Distance(me, enemies[1]));
                float dis2 = Mathf.Sqrt(Distance(me, enemies[2]));
                int isDead0 = ((int)Jenemies[0]["state"] & 0x01);
                int isDead1 = ((int)Jenemies[1]["state"] & 0x01);
                int isDead2 = ((int)Jenemies[2]["state"] & 0x01);
                float val0 = hp0 + dis0 + isTyor0 * 10 + isDead0 * 99999;
                float val1 = hp1 + dis1 + isTyor1 * 10 + isDead1 * 99999;
                float val2 = hp2 + dis2 + isTyor2 * 10 + isDead2 * 99999;
                if (isDead0 == 1 && isDead1 == 1 && isDead2 == 1)
                {
                    eat = true;
                    return;
                }
                if (val0 <= val1 && val0 <= val2)
                {
                    enemie_index = (int)Jenemies[0]["index"];
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                    return;
                }
                if (val1 <= val0 && val1 <= val2)
                {
                    enemie_index = (int)Jenemies[1]["index"];
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                    return;
                }
                if (val2 <= val1 && val2 <= val0)
                {
                    enemie_index = (int)Jenemies[2]["index"];
                    eat = false;
                    step_1 = false;
                    step_2 = true;
                    step_3 = false;
                    step_4 = false;
                    step_5 = false;
                }
            }
            if (Jenemies.Count == 0 && Jtargets.Count == 0) UseSkill(0);
        }


        /*if (step_3)
        {
            float temp = (float)99999.0;
            int now_i = -1;
            if (Jenemies.Count > 0)
            {
                for (int i = 0; i < Jenemies.Count; ++i)
                {
                    if (((int)Jenemies[i]["state"] & 0x01) == 0 && Distance(me, enemies[i]) < temp)
                    {
                        now_i = i;
                        temp = Distance(me, enemies[i]);
                    }
                }
            }
            if (now_i != -1 && hp > 25.0 && (float)Jenemies[now]["hp"] < (hp + 10))
            {
                enemie_index = (int)Jenemies[now]["index"];
                first_step = false;
                step_1 = false;
                step_2 = true;
                step_3 = false;                
                step_4 = false;
            }
            else
            {
                //if(hp<=25 || now==-1)
                {
                    int target = -1;
                    if (Jtargets.Count > 0)
                    {
                        int minest = 0;
                        float minDis = Distance(state["me"], targets[0]);
                        for (int i = 1; i < Jtargets.Count; ++i)
                        {
                            if (Distance(state["me"], targets[i]) < minDis)
                            {
                                minest = i;
                                minDis = Distance(state["me"], targets[i]);
                            }
                        }
                        target = minest;
                        Move((float)Jtargets[minest]["pos"]["x"], (float)Jtargets[minest]["pos"]["z"]);
                        end_x = (float)Jtargets[minest]["pos"]["x"];
                        end_z = (float)Jtargets[minest]["pos"]["z"];
                    }
                    if (Mathf.Abs(x - end_x) < DEVIATION && Mathf.Abs(z - end_z) < DEVIATION)
                        isArrive = true;
                    else isArrive = false;
                    //Move(end_x, end_z);
                    if (target != -1)
                    {
                        if (Distance(me, targets[target]) <= shootRange)
                        {
                            UseSkill(0);
                            target = -1;
                        }
                    }
                    if (Jtargets.Count == 0 && isArrive)
                    {
                        go_another_area(state);
                    }
                    if (now != -1 && temp < 16 * 16)
                    {
                        UseSkill(1, (int)Jenemies[now]["index"]);
                    }
                }
            }
        }
        */

        if (step_4 && ((int)me["state"] & 0x01) == 0)
        {
            Move(death_x, death_z);
            end_x = death_x;
            end_z = death_z;
            step_1 = true;
            step_2 = false;
            step_3 = false;
            step_4 = false;
            step_5 = false;
        }

        if (step_5)
        {
            int target = -1;
            if (Jenemies.Count > 0)
            {
                float t_dis = 99999f;
                int t_i = 0;
                for (int i = 0; i < Jenemies.Count; ++i)
                {
                    if (Distance(me, enemies[i]) <= t_dis)
                    {
                        t_dis = Distance(me, enemies[i]);
                        t_i = i;
                    }
                }
                if (t_dis <= 16 * 16){
                    UseSkill(1, (int)Jenemies[t_i]["index"]);
                    if (((int)state["me"]["state"] & 0x08) == 0) UseSkill(0);
                }

            }
            if (Jtargets.Count > 0 && Jenemies.Count==0)
            {
                int minest = 0;
                float minDis = Distance(state["me"], targets[0]);
                for (int i = 1; i < Jtargets.Count; ++i)
                {
                    if (Distance(state["me"], targets[i]) < minDis)
                    {
                        minest = i;
                        minDis = Distance(state["me"], targets[i]);
                    }
                }
                target = minest;
                Move((float)Jtargets[minest]["pos"]["x"], (float)Jtargets[minest]["pos"]["z"]);
                end_x = (float)Jtargets[minest]["pos"]["x"];
                end_z = (float)Jtargets[minest]["pos"]["z"];
                if (target != -1)
                {
                    if (Distance(me, targets[target]) <= 3 * 3)
                    {
                        UseSkill(0);
                    }
                }   
            }

            if (Jpick.Count > 0)
            {
                float minDis_p = 99999;
                int pick_i = 0;
                for (int i = 0; i < Jpick.Count; ++i)
                {
                    if (Distance(state["me"], pick[i]) < minDis_p)
                    {
                        pick_i = i;
                        minDis_p = Distance(state["me"], pick[i]);
                    }
                }
//                if (Jenemies.Count>0) UseSkill(0);
                end_x = (float)Jpick[pick_i]["pos"]["x"];
                end_z = (float)Jpick[pick_i]["pos"]["z"];

                Move(end_x, end_z);
            }

            if (Jtargets.Count ==0 && Jpick.Count == 0  && isArrive)
            {
                go_another_area(state);
            }



            //判断火女大招
            int Hi = -1;
            for (int i = 0; i < misc.Count; i++)
                if ((string)misc[i]["type"] == "meteorite" && ((float)misc[i]["pos"]["x"] - x) * ((float)misc[i]["pos"]["x"] - x) + ((float)misc[i]["pos"]["z"] - z) * ((float)misc[i]["pos"]["z"] - z) < 10 * 10)
                {
                    Hi = i;
                    break;
                }
            if (Hi != -1)
            {
                float x1, x2, x3, z1, z2, z3, ang;
                x1 = (float)misc[Hi]["pos"]["x"];
                z1 = (float)misc[Hi]["pos"]["z"];
                //x2 = (float)Jenemies[now]["pos"]["x"];
                //z2 = (float)Jenemies[now]["pos"]["z"];
                x2 = (float)me["pos"]["x"];
                z2 = (float)me["pos"]["z"];
                ang = Mathf.Atan2(z2 - z1, x2 - x1);
                ang = Moderate(ang);
                x3 = x1 + 11.5f * Mathf.Cos(ang);
                z3 = z1 + 11.5f * Mathf.Sin(ang);
                UseSkill(0);
                Move(x3, z3);
                end_x = x3;
                end_z = z3;
            }
        }

    }



    //判断对面是否在逃跑
    private bool isLeaf(JToken me, JToken enemies)
    {
        float x1 = (float)me["pos"]["x"], z1 = (float)me["pos"]["z"];
        float x2 = (float)enemies["pos"]["x"], z2 = (float)enemies["pos"]["z"];
        float x3 = x2 + (float)enemies["vel"]["x"], z3 = z2 + (float)enemies["vel"]["z"];
        float r0 = (x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1);
        float r1 = (x3 - x1) * (x3 - x1) + (z3 - z1) * (z3 - z1);
        return r1 >= r0;
    }

    private float Distance(JToken a, JToken b)
    {
        float dx = (float)a["pos"]["x"] - (float)b["pos"]["x"];
        float dz = (float)a["pos"]["z"] - (float)b["pos"]["z"];
        return dx * dx + dz * dz;
    }
    private void go_another_area(JObject state)
    {
        var me = state["me"];
        float x = (float)me["pos"]["x"];
        float z = (float)me["pos"]["z"];
        if (x <= 50 && z <= 50)
        {
            Move(75, 25);
            end_x = 75;
            end_z = 25;
        }
        else if (x <= 50 && z >= 50)
        {
            Move(25, 25);
            end_x = 25;
            end_z = 25;
        }
        else if (x >= 50 && z <= 50)
        {
            Move(75, 75);
            end_x = 75;
            end_z = 75;
        }
        else //if (x >= 50 && z >= 50)
        {
            Move(25, 75);
            end_x = 25;
            end_z = 75;
        }
    }
}
