﻿function edgarServ($http) {
    
    var URL_GET_ALL_DATASETS = "api/alldatasets"
    var URL_PROCESS_DATASET = "api/processdataset";
    var URL_DELETE_DATASET_FILE = "api/deletedataset";
    var URL_GET_FULL_INDEXES = "api/fullindexes";
    var URL_PROCESS_FULL_INDEX = "api/processfullindex";
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Private
    var getPromise = function (pUrl) {
        var promise = $http({
            method: "GET",
            url: pUrl,
            cache: false
        });
        promise.success(
            function (data, status) {
                return data;
            }
       );

        promise.error(
            function (data, status) {
                console.log(data, status);
                return { "status": false };
            }
        );
        return promise;
    };

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Public

    ////////////////////////////////
    //Public - Datasets
    this.getDatasets = function (successCallback, errorCallback) {
        getPromise(URL_GET_ALL_DATASETS).then
        (
            function success(response) {
                successCallback(response.data);
            },
            function error(response) {
                errorCallback(response);
            }
        );
    };

    this.processDataset = function (id, callbackSuccess, callbackError) {
        $http.post(URL_PROCESS_DATASET, '"' + id + '"').success(callbackSuccess).error(callbackError);
    };

    this.deleteDataset = function (id, file, callbackSuccess, callbackError) {
        var data = { 'id': id, 'file': file };
        $http.post(URL_DELETE_DATASET_FILE, data).success(callbackSuccess).error(callbackError);
    }

    ////////////////////////////////
    //Public - Files

    this.getFullIndexes = function (successCallback, errorCallback) {
        getPromise(URL_GET_FULL_INDEXES).then
            (
            function success(response) {
                successCallback(response.data);
            },
            function error(response) {
                errorCallback(response);
            }
            );
    };

    this.processFullIndex = function (year, quarter, callbackSuccess, callbackError) {
        var data = { 'year': year, 'quarter': quarter };
        $http.post(URL_PROCESS_FULL_INDEX, data).success(callbackSuccess).error(callbackError);
    };
}