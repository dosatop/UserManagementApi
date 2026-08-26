using UserManagementApi.DTOs.Results;

namespace UserManagementApi.Services.Interfaces;

public interface IResultService
{
    // ================================================================
    // GET RESULTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            GetTeacherResultsRequest request);


    // // ================================================================
    // // GET SINGLE RESULT
    // // ================================================================

    // Task<(bool Success, object? Data, string? Error)>
    //     GetResultByIdAsync(
    //         string userId,
    //         Guid resultId);


    // // ================================================================
    // // CREATE RESULT
    // // ================================================================

    // Task<(bool Success, object? Data, string? Error)>
    //     CreateResultAsync(
    //         string userId,
    //         CreateResultRequest request);


    // // ================================================================
    // // UPDATE RESULT
    // // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            UpdateResultRequest request);


    // // ================================================================
    // // DELETE RESULT
    // // ================================================================

   Task<(bool Success, object? Data, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId);


    // ================================================================
    // TEACHER - BULK EXAM RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        BulkExamResultAsync(
            string userId,
            BulkExamResultRequest request);


    // ================================================================
    // TEACHER - BULK TEST RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        BulkTestResultAsync(
            string userId,
            BulkTestResultRequest request);


    // ================================================================
    // TEACHER - BULK TEST + EXAM RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        BulkResultAsync(
            string userId,
            BulkResultRequest request);


    // ================================================================
    // SINGLE TEST RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UploadTestResultAsync(
            string userId,
            UploadTestResultRequest request);


    // ================================================================
    // SINGLE EXAM RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UploadExamResultAsync(
            string userId,
            UploadExamResultRequest request);


    // ================================================================
    // SINGLE COMPLETE RESULT
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UploadResultAsync(
            string userId,
            UploadResultRequest request);


    // ================================================================
    // UPDATE TEST
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UpdateTestResultAsync(
            string userId,
            Guid resultId,
            UpdateTestResultRequest request);


    // ================================================================
    // UPDATE EXAM
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        UpdateExamResultAsync(
            string userId,
            Guid resultId,
            UpdateExamResultRequest request);


    // ================================================================
    // IMPORT - PREVIEW
    // ================================================================

    // Task<(
    //     bool Success,
    //     ResultImportPreviewResponse? Data,
    //     string? Error
    // )>
    //     PreviewAsync(
    //         string userId,
    //         ImportResultsRequest request);


    // // ================================================================
    // // IMPORT - CONFIRM
    // // ================================================================

    // Task<(
    //     bool Success,
    //     object? Data,
    //     string? Error
    // )>
    //     ConfirmImportAsync(
    //         string userId,
    //         ConfirmResultImportRequest request);
}
