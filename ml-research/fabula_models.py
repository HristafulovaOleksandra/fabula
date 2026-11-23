import numpy as np
import pandas as pd
from sklearn.base import BaseEstimator, ClassifierMixin, clone

N_LEVELS = 5


class RuleBaseline(BaseEstimator, ClassifierMixin):
    def fit(self, features: pd.DataFrame, labels) -> "RuleBaseline":
        labels = np.asarray(labels)
        medians = pd.Series(features["n_words"].to_numpy()).groupby(labels).median().sort_index().to_numpy()
        self.length_thresholds_ = np.maximum.accumulate((medians[:-1] + medians[1:]) / 2)
        self.classes_ = np.arange(N_LEVELS)
        return self

    def predict(self, features: pd.DataFrame) -> np.ndarray:
        length_level = np.searchsorted(self.length_thresholds_, features["n_words"].to_numpy(), side="right")
        word_level = np.clip(features["max_word_level"].to_numpy(), 0, N_LEVELS - 1)
        return np.floor((length_level + word_level) / 2 + 0.5).astype(int)


class RoundedRegressor(BaseEstimator, ClassifierMixin):
    def __init__(self, regressor=None):
        self.regressor = regressor

    def fit(self, features, labels) -> "RoundedRegressor":
        self.regressor_ = clone(self.regressor).fit(features, labels)
        self.classes_ = np.arange(N_LEVELS)
        return self

    def predict(self, features) -> np.ndarray:
        return np.clip(np.rint(self.regressor_.predict(features)), 0, N_LEVELS - 1).astype(int)
