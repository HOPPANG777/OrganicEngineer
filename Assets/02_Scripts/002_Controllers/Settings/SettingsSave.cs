using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// 기능: SettingsSave
// 설명: SettingsData JSON 파일 로드/저장과 마이그레이션 후 재저장 여부 판단을 담당한다.
//==============================================================================
// TODO: 백업 파일 생성, 저장 실패 재시도
//
//==============================================================================
public static class SettingsSave
{
    //==============================================================================
    // 기능: SettingsSave::LoadAsync
    // 설명: 저장 파일을 읽고 없거나 실패하면 기본값으로 복구한 뒤 결과를 반환한다.
    //==============================================================================
    // TODO: JsonUtility 대신 버전 호환성이 좋은 serializer 검토
    //
    //==============================================================================
    public static async UniTask<SettingsLoadResult> LoadAsync(string path, SettingsConfig config, CancellationToken ct = default)
    {
        // Config 가 없으면 빈 데이터로 복구
        if (config == null)
        {
            Debug.LogError("[SettingsSave] SettingsConfig가 비어있습니다.");
            return new SettingsLoadResult(new SettingsData(), false);
        }

        // 파일이 없으면 기본값으로 시작
        if (!File.Exists(path))
        {
            return new SettingsLoadResult(config.CreateDefaultData(), false);
        }

        bool needsResave = false;
        SettingsData data;

        try
        {
            // JSON 파일 읽기
            string json = await File.ReadAllTextAsync(path, ct);
            SettingsData loaded = JsonUtility.FromJson<SettingsData>(json);

            // 파싱 실패 시 기본값으로 복구
            if (loaded == null)
            {
                Debug.LogWarning("[SettingsSave] 저장 파일 파싱 실패. 기본값으로 복구합니다.");
                data = config.CreateDefaultData();
                needsResave = true;
            }
            else
            {
                // 구버전이면 현재 버전으로 마이그레이션
                int prevVersion = loaded.Version;
                if (loaded.MigrateToCurrent())
                {
                    Debug.Log($"[SettingsSave] 설정 스키마 마이그레이션: v{prevVersion} -> v{SettingsData.CURRENT_VERSION}");
                    needsResave = true;
                }

                // 로드 직후 값 범위 보정
                loaded.ClampWith(config);
                data = loaded;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            // 예외 발생 시 기본값으로 복구
            Debug.LogError($"[SettingsSave] 설정 로드 실패: {e.Message}. 기본값으로 복구합니다.");
            data = config.CreateDefaultData();
            needsResave = true;
        }

        return new SettingsLoadResult(data, needsResave);
    }

    //==============================================================================
    // 기능: SettingsSave::SaveAsync
    // 설명: 현재 SettingsData 를 JSON 으로 변환해 지정 경로에 저장한다.
    //==============================================================================
    // TODO: 임시 파일 쓰기 후 교체 방식으로 안전 저장
    //
    //==============================================================================
    public static async UniTask SaveAsync(string path, SettingsData data, SettingsConfig config, CancellationToken ct = default)
    {
        // 저장할 데이터가 없으면 종료
        if (data == null)
        {
            Debug.LogWarning("[SettingsSave] 저장할 데이터가 없습니다.");
            return;
        }

        try
        {
            // 저장 직전에도 값 범위 보정
            data.ClampWith(config);

            // JSON 직렬화
            string json = JsonUtility.ToJson(data, prettyPrint: true);

            // 저장 폴더 보장
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 파일 쓰기
            await File.WriteAllTextAsync(path, json, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsSave] 설정 저장 실패: {e.Message}");
        }
    }
}

//==============================================================================
// 기능: SettingsLoadResult
// 설명: 설정 로드 결과 데이터와 최신 스키마 재저장 필요 여부를 담는다.
//==============================================================================
// TODO:
//
//==============================================================================
public readonly struct SettingsLoadResult
{
    // 로드 또는 복구된 설정 데이터
    public readonly SettingsData Data;

    // 마이그레이션/복구로 재저장이 필요한지 여부
    public readonly bool NeedsResave;

    public SettingsLoadResult(SettingsData data, bool needsResave)
    {
        Data = data;
        NeedsResave = needsResave;
    }
}
